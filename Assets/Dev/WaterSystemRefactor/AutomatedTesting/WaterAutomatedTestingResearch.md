# Automated Testing Research for the Dev Water System Refactor

**Date:** 2026-08-26  
**Scope:** Research and recommendation only. No production code or test code was changed by this report.

## Executive recommendation

Use the Unity Test Framework (UTF) already present in this project, with NUnit-style tests and the Unity Test Runner as the normal developer interface. Do not introduce a second unit-test framework.

The recommended design is a small test pyramid:

1. **EditMode unit tests** for the plain C# water units and deterministic numerical behavior. These should be the majority of the suite and should use NUnit `[Test]` methods.
2. **EditMode scenario tests** for the three requested edge cases. They can use the same deterministic in-memory map/state builders as the unit tests, but should be categorized and named as gameplay scenarios.
3. **PlayMode integration tests** for `MonoBehaviour` lifecycle, `Start`/`Update`, event delivery, controller orchestration, and actual Tilemap rendering. Use `[UnityTest]` only where a frame or Unity runtime lifecycle is genuinely required.

Run tests interactively from **Window > General > Test Runner**. Run them from the command line for repeatability/CI, writing NUnit XML with `-testResults`. This gives developers an immediate green/red tree and gives automation a machine-readable report.

The project is on Unity `6000.0.63f1` and pins `com.unity.test-framework` to `1.6.0` in `Packages/manifest.json`. UTF is the Unity-supported framework for EditMode, PlayMode, and player tests, and its package documentation describes the NUnit integration and test-runner workflow: [UTF 1.6 overview](https://docs.unity.cn/Packages/com.unity.test-framework@1.6/manual/index.html).

## What the repository implies

The refactor is a good fit for focused tests because its runtime responsibilities are already separated:

| Area | Current refactor types | Recommended first test mode |
| --- | --- | --- |
| Map access and coordinate conversion | `MapDef`, `MapCellDef`, `MapAccessor` | EditMode unit |
| Mutable state and dirty tracking | `WaterState` | EditMode unit |
| Numerical settings, sources, modifiers, summaries, projections | `WaterTypes`, `WaterProjection` | EditMode unit |
| Flow, sources, drainage, barriers, active-region gating | `WaterPhysics`, `WaterPhysicsBarrier` | EditMode unit/scenario |
| Scenario validation and cloning | `ScenarioDef`, `WaterStormProfile` | EditMode unit |
| Public orchestration and profile changes | `WaterController` | EditMode where possible; PlayMode for Unity lifecycle |
| Game-flow/phase callbacks and one-time preliminary flooding | `WaterLifecycleCoordinator` | PlayMode integration |
| Tilemap writes and renderer lifecycle | `WaterRenderer`, `RendererDef` | EditMode band-resolution unit; PlayMode Tilemap integration |
| Projection refresh/event subscription | `ProjectionController` | PlayMode integration |

The core state, physics, barrier, accessor, and data types are ordinary C# objects. They do not need a scene, real time, a camera, or a running `MonoBehaviour`; keeping those tests in EditMode makes them faster and less brittle. The controller, lifecycle coordinator, renderer, and projection controller are Unity components, so a smaller PlayMode layer should verify their runtime seams.

At review time there are no project `.asmdef` files under `Assets`. That matters because UTF test assemblies need explicit references to the code assembly they test. The clean long-term setup is to give `Assets/Game/Features/WaterSystemRefactor/Scripts` a runtime assembly definition, then have test assemblies reference it. Unity documents assembly definitions as a way to control dependencies and reduce unnecessary recompilation: [Organizing scripts into assemblies](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-files.html).

## Test-writing method

### Arrange, Act, Assert

Use **Arrange, Act, Assert (AAA)** in every test. Unity’s own UTF exercise describes AAA as a clear separation between setup, the operation under test, and evaluation: [UTF AAA exercise](https://docs.unity.cn/Packages/com.unity.test-framework@1.6/manual/course/arrange-act-assert.html). Microsoft’s .NET guidance likewise recommends isolated, repeatable, self-checking tests, one Act task, and inputs limited to what the behavior requires: [Unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices).

For this system, a physics test should look conceptually like this:

```text
Arrange: build a small map, state, barrier grid, settings, sources, and modifiers.
Act: call one public operation, normally Initialize, ApplyInitialSources, or Step.
Assert: check the intended result and the relevant invariant.
```

Keep the Act section short. Do not reproduce the water algorithm in the test as a second implementation; that can make a test agree with the same bug as production code. Prefer hand-calculable fixtures and invariants such as conservation, non-negativity, finite values, saturation, or event count.

### Focus behavior through public seams

Test public behavior rather than private helper methods. For example, test `WaterPhysics.Step` for the combined effect of flow acceleration, outflow scaling, drainage, boundary cleanup, and summary creation. Test `WaterController` through `InitializeRuntimeState`, `StepSimulation`, `RunSimulationForDuration`, profile/lifecycle methods, and public queries. This follows Microsoft’s guidance that private methods are implementation details and are usually better verified through the public method that uses them: [Validate private methods through public methods](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices#validate-private-methods-with-public-methods).

Use a small test-only builder/helper for repeated map creation, but keep each test’s meaningful inputs visible. Avoid a large implicit `[SetUp]` fixture that configures every possible field for every test. NUnit does support per-test `[SetUp]`/`[TearDown]`; use them only for genuinely universal cleanup or isolation, as documented in [NUnit SetUp](https://docs.nunit.org/articles/nunit/writing-tests/attributes/setup.html).

### Example-based and parameterized tests

Use ordinary named `[Test]` methods for important behavior and `[TestCase]`/`[TestCaseSource]` for compact variations of the same behavior. NUnit’s `[TestCase]` creates a distinct test for each supplied argument set, and `[TestCaseSource]` keeps richer scenario data separate from the test body: [NUnit TestCase](https://docs.nunit.org/articles/nunit/writing-tests/attributes/testcase.html), [NUnit TestCaseSource](https://docs.nunit.org/articles/nunit/writing-tests/attributes/testcasesource.html).

Good parameterized candidates include:

- invalid settings values (`NaN`, infinity, zero, negative, and boundary-valid values);
- map coordinates inside, outside, and on each edge;
- source kinds and modifier scaling combinations;
- drainage greater than, equal to, and less than the input rate;
- initial depth just below, exactly at, and just above the active-region threshold.

Do not make a combinatorial matrix by default. UTF’s test-case documentation warns that combining multiple value sources can create many cases with little value: [UTF test-case exercise](https://docs.unity.cn/Packages/com.unity.test-framework@1.6/manual/course/test-cases.html). Prefer a named `ScenarioCase` data object with only the combinations that represent a meaningful contract.

For floating-point results, use an explicit tolerance tied to the simulation’s numerical scale. NUnit’s equality constraint supports `.Within(...)` for floating-point comparisons: [NUnit Equal Constraint](https://docs.nunit.org/articles/nunit/writing-tests/constraints/EqualConstraint.html). Also assert exact discrete values such as `StepIndex`, wet-tile count, event count, and boundary/active flags where those are the contract.

## Proposed test taxonomy

The following is a practical first-pass taxonomy. It is intentionally organized by behavior and public seam, rather than by mirroring every private method.

### `MapAccessorTests` — focused map unit tests

- Origin, width, height, and simulation-to-tile coordinate conversion.
- Tile-to-simulation round trips for normal cells and rejection of outside coordinates.
- Missing/nonexistent cells do not appear as simulation cells.
- Elevation, initial-water-body, and renderer-definition lookups return the correct cell data or safe defaults.
- Non-square maps and non-zero origins, since indexing bugs often hide in square origin-zero fixtures.

### `WaterStateTests` — state and mutation unit tests

- Grid dimensions include the expected guard border.
- Terrain and initial water are snapshotted from the map; live state is separate from authored map data.
- `TrySetWaterDepth` rejects invalid/outside cells and clamps negative depth to zero through the intended public contract.
- Dirty cells are marked once, can be cleared, and do not include nonexistent map cells.
- `Clone` is independent: changing cloned water, flow, active flags, or dirty state does not change the live state. If internal clone access is needed, expose it through a public projection behavior or an explicit test seam instead of using reflection.

### `WaterTypesTests` and `ScenarioDefTests` — data/validation unit tests

- `Sanitize` produces the documented lower/upper bounds for settings and modifier snapshots.
- `IsValid` rejects non-finite values and invalid durations/source depths with useful failure results.
- Profile/source cloning prevents a simulation from mutating scenario-authored data.
- Missing profiles, missing sources, and invalid preliminary-flooding configuration fail in the expected way.
- Projection bounds return zero outside the map and preserve an immutable copy of supplied depths.

### `WaterPhysicsTests` — focused physics unit tests

- Initialization clears flow history, applies boundary-wall setup when enabled, and initializes the active region according to the threshold.
- Each source region is correct: full map/rainfall, deduplicated edges, corners, and initial water bodies.
- Initial sources use absolute depth; continuous sources use depth per simulated second and scale by `dt`.
- Rainfall, external load, and antecedent wetness modifiers scale only when configured.
- A closed map with no sources, drainage, wind, or barriers conserves total water within an explicit numerical tolerance.
- Water never becomes negative or non-finite; configured positive `maxWaterDepth` is not exceeded.
- Active-region gating expands only to valid map cells, respects spread interval/layer settings, and does not create water on nonexistent cells.
- Barriers cover blocked flow, zero seepage, finite seepage, and partial/full overtopping transmission in both X and Y directions.
- `WaterStepSummary` reports the correct step index, simulated delta, wet count, total water, maximum depth, and dirty count.

### `WaterControllerTests` — public orchestration tests

- Missing map/scenario/modifier provider behavior matches `Production` versus `DevDefaultsWithWarnings` configuration.
- Initialization, begin, pause, resume, reset, and repeated calls have the expected boolean results and event counts.
- `RunSimulationForDuration` divides time into complete microsteps and rejects non-finite/non-positive durations.
- Crisis profile selection, game-flow gating, and game-phase transitions match the cutover checklist.
- Public setters reject invalid values and do not mutate state on failure.
- A projection uses a cloned state and leaves live depth, flow, barriers, and dirty state unchanged.

### `WaterLifecycleCoordinatorTests` — lifecycle integration tests

- Loading initializes the controller once the map/scenario contract is ready.
- A completed preparation turn below the configured threshold does nothing.
- The threshold turn runs preliminary flooding exactly once.
- Repeated notifications and re-entrant notification cannot schedule the batch twice.
- A new run clears the one-time marker and resets the water controller.

### Renderer/projection tests

- `RendererDef` resolves dry, shallow, deep, null-band, and boundary-depth cases deterministically.
- A PlayMode test with a minimal Tilemap checks that dirty cells are applied to the configured tilemap and that rendering does not write back into simulation state.
- Projection events are coalesced during a transaction and refreshed after the transaction ends.

## The three requested scenario tests

These should be named as scenarios and placed in a `WaterScenario` category, even if their implementation calls `WaterPhysics` directly. That makes them easy to run as a meaningful group while retaining deterministic unit-test speed.

### 1. Almost no water at start

Use a small non-square map with exact-zero water, a tiny positive depth, the threshold itself, and a value just above the threshold. Test both `useSpreadGating = true` and `false` where the gameplay contract differs.

The current code defines activation strictly as `water > expandFromWaterThreshold`; with gating enabled, an inactive cell with positive water is cleared during the next update. That is a material gameplay contract, not merely an implementation detail. The test should therefore either:

- lock in that tiny sub-threshold water is intentionally treated as dry and disappears on the next gated step; or
- expose a defect if “almost no water” is expected to persist and become active later.

Regardless of that product decision, assert that initialization and stepping produce no negative/non-finite depth, do not activate nonexistent cells, and report consistent wet counts. Include exact threshold values because the strict comparison is an edge condition.

Suggested test name: `InitializeAndStep_NearZeroStartingWater_UsesDocumentedActivationPolicy`.

### 2. Very heavy external inflow

Use a 3x3 or 5x3 map, a continuous `Edges`/`Boundary` source, a very large source rate and external-water-load modifier, and a finite positive `maxWaterDepth`. Set gravity, wind, and drainage to zero in the exact saturation test so the expected input is hand-calculable.

Assert that:

- source contribution is scaled by simulated `dt` and external load;
- only logical edge cells receive the boundary source, with corners not double-counted;
- each cell remains finite and at or below `maxWaterDepth`;
- saturation is stable over repeated steps and does not overflow or produce `NaN`;
- the guard border remains dry and no nonexistent map cell receives water;
- `WaterStepSummary` agrees with the state.

Suggested test name: `Step_HeavyExternalInflow_SaturatesWithoutNonFiniteWater`.

Use a second variant with `maxWaterDepth = 0` only if unlimited depth is an intentional supported mode; the current implementation treats that value specially, so it should be an explicit contract rather than an accidental test default.

### 3. High rain with high drainage

Use a small all-valid map, disable flow variables for the exact arithmetic test, seed a known positive depth, configure a `Rainfall` source in depth-per-second units, and set high rainfall and drainage-efficiency modifiers. Run cases where net input is positive, zero, and negative.

For one step, the expected per-cell result is conceptually:

```text
clamp(startDepth + rainfallRate * sourceRate * dt
      - baseDrainageDepthPerSecond * drainageEfficiency * dt,
      0, maxWaterDepth)
```

The source is applied before drainage in the current implementation. Encode that order in the exact one-step test, then add a multi-step stability test that verifies no negative depth and sensible convergence/decline. Keep the test’s units explicit: rainfall and continuous source `depth` are rates, while initial sources are absolute depths.

Suggested test names:

- `Step_HighRainAndHighDrainage_AppliesNetChangeWithoutGoingNegative`
- `Step_HighRainAndHighDrainage_WhenDrainExceedsInput_ClampsAtZero`
- `Step_HighRainAndHighDrainage_WhenInputExceedsDrainage_RemainsBounded`

## EditMode, PlayMode, and test assemblies

UTF describes EditMode tests as editor-run tests with access to game code and editor code, and PlayMode tests as runtime tests that can run in the Editor or a player. Its guidance recommends NUnit `[Test]` unless a test must yield, skip a frame, or wait: [Edit Mode vs. Play Mode tests](https://docs.unity.cn/Packages/com.unity.test-framework@1.6/manual/edit-mode-vs-play-mode-tests.html).

For this project, use this proposed structure when implementation begins:

```text
Assets/Game/Features/WaterSystemRefactor/
  Scripts/
    WaterSystemRuntime.asmdef          # runtime seam for the refactor
  AutomatedTesting/
    EditMode/
      WaterSystemEditModeTests.asmdef  # NUnit + Unity test references
      *Tests.cs
    PlayMode/
      WaterSystemPlayModeTests.asmdef  # NUnit + Unity test references
      *Tests.cs
```

The UTF workflow creates a test assembly with references to `nunit.framework.dll`, `UnityEngine.TestRunner`, and, for EditMode tests, `UnityEditor.TestRunner`: [Creating test assemblies](https://docs.unity.cn/Packages/com.unity.test-framework@1.6/manual/workflow-create-test-assembly.html). Add the runtime assembly reference explicitly to each test assembly. Keep EditMode and PlayMode tests in separate assemblies for the project’s 1.x package line; the later UTF 2.0 documentation identifies combining them as a new change that removes the previous separation requirement: [UTF 2.0 changes](https://docs.unity.cn/Packages/com.unity.test-framework@2.0/manual/whats-new.html).

Do not enable PlayMode tests for every predefined assembly as the default solution. That makes dependencies broad and can increase build size/time. A dedicated runtime assembly is clearer, keeps the test boundary explicit, and makes future migration from `Dev` to shippable feature code easier.

## Developer-facing run interface and reporting

### In the Unity Editor

The Test Runner window should be the first interface developers use:

1. Open **Window > General > Test Runner**.
2. Run one test, a fixture, **Run Selected**, or **Run All**.
3. Filter by fixture/name/category and inspect the green pass or red failure result, assertion message, and stack trace.

UTF documents double-clicking tests/fixtures, Run All, Run Selected, context-menu Run, and filtering in the Test Runner: [Running tests](https://docs.unity.cn/Packages/com.unity.test-framework@1.6/manual/workflow-run-test.html). This is simpler for daily development than a custom editor window or a bespoke console runner.

Use descriptive names with the method, scenario, and expected behavior, for example `Step_HeavyExternalInflow_SaturatesWithoutNonFiniteWater`. Apply a small stable category vocabulary:

- `WaterUnit` — focused map/state/data/physics tests;
- `WaterScenario` — the three gameplay edge cases and other deterministic scenarios;
- `WaterLifecycle` — controller/coordinator/profile transitions;
- `WaterRendering` — renderer and Tilemap integration;
- `WaterSlow` — only tests that genuinely need a scene, many frames, or a player.

NUnit categories are designed to group tests and include/exclude them from runners: [NUnit Category](https://docs.nunit.org/articles/nunit/writing-tests/attributes/category.html).

### Command line

Use the Unity executable matching `6000.0.63f1`. The initial practical commands are:

```powershell
& "<Unity 6000.0.63f1>\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\Users\hmark\flood-game" `
  -runTests -testPlatform EditMode `
  -assemblyNames "WaterSystemEditModeTests" `
  -testResults "Temp\WaterSystem_EditMode.xml"
```

```powershell
& "<Unity 6000.0.63f1>\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\Users\hmark\flood-game" `
  -runTests -testPlatform PlayMode `
  -assemblyNames "WaterSystemPlayModeTests" `
  -testResults "Temp\WaterSystem_PlayMode.xml"
```

For focused runs, add either `-testFilter "WaterPhysicsTests"` or a category filter such as `-testCategory "WaterScenario"`. UTF documents `-runTests`, `-batchmode`, `-testPlatform`, `-assemblyNames`, `-testFilter`, `-testCategory`, and `-testResults`: [UTF command-line reference](https://docs.unity.cn/Packages/com.unity.test-framework@1.6/manual/reference-command-line.html). Omit `-testPlatform` only when the default EditMode behavior is wanted.

`-testResults` writes NUnit-format XML. The result format contains suite/case names, pass/fail/skipped results, counts, durations, failure messages, and stack traces: [NUnit Test Result XML](https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html). Treat this XML as the durable CI artifact. Unity’s command-line documentation notes that there is not one common exit-code definition for every Unity component under test, so an automation wrapper should validate the XML’s failed/error counts and the Unity log rather than trusting only a process exit code.

For a later one-click launcher, UTF also exposes `TestRunnerApi` filters and callbacks for programmatic runs and result handling: [Run tests programmatically](https://docs.unity.cn/Packages/com.unity.test-framework@1.6/manual/extension-run-tests.html), [Get test results](https://docs.unity.cn/Packages/com.unity.test-framework@1.6/manual/extension-get-test-results.html). This is optional; the Test Runner window plus the standard CLI is sufficient for the first implementation.

## Practical workflow

1. Add the runtime assembly definition and two test assemblies under this refactor’s `AutomatedTesting` folder.
2. Start with `MapAccessorTests`, `WaterStateTests`, `WaterTypesTests`, and validation tests. These should run quickly in EditMode.
3. Add focused physics tests with small hand-built maps and explicit float tolerances.
4. Add the three `WaterScenario` tests, locking down the near-zero threshold policy before treating the test as a permanent gameplay contract.
5. Add controller/profile/lifecycle tests. Use PlayMode only for `MonoBehaviour` execution, frame timing, scene state, and Tilemap integration.
6. In the Editor, run the changed fixture while developing, then Run All EditMode tests before review. Run PlayMode tests when controller, lifecycle, renderer, scene, or Unity callback behavior changes.
7. In automation, run EditMode and PlayMode separately and save their XML reports as build artifacts. Filter by `WaterUnit` for fast feedback and run the full suite as the verification gate.
8. If a test fails, use its descriptive name, assertion message, and XML stack trace to identify the violated behavior. Do not weaken tolerances or add retries to hide nondeterminism; investigate the state reset, time step, source units, or numerical contract first.

This approach gives the project specific, repeatable tests for the refactor’s code units, explicit coverage for the requested real-world extremes, and a clear path from a developer’s Test Runner click to machine-readable CI results.
