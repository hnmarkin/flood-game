# Dev Water Test Runbook

The water suite uses Unity Test Framework 1.6.0 in Unity `6000.0.63f1`.

## Test Runner

Open **Window > General > Test Runner**. Run the `WaterSystemRefactor.EditModeTests` assembly in EditMode, then `WaterSystemRefactor.PlayModeTests` in PlayMode. Deterministic unit and scenario tests must appear only in EditMode; lifecycle and rendering tests must appear only in PlayMode.

Categories support focused runs:

- `WaterUnit`: map, state, configuration, physics, and immutable values
- `WaterScenario`: near-zero water, heavy external inflow, and rainfall/drainage balances
- `WaterLifecycle`: controller, lifecycle coordinator, and projection controller
- `WaterRendering`: transient Tilemap integration

## Command line

From PowerShell at the repository root, with the Unity Editor closed:

```powershell
New-Item -ItemType Directory -Force 'TestResults' | Out-Null

& 'C:\Program Files\Unity\Hub\Editor\6000.0.63f1\Editor\Unity.exe' -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform EditMode -testResults 'TestResults\water-editmode.xml' -logFile 'TestResults\water-editmode.log'

& 'C:\Program Files\Unity\Hub\Editor\6000.0.63f1\Editor\Unity.exe' -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform PlayMode -testResults 'TestResults\water-playmode.xml' -logFile 'TestResults\water-playmode.log'
```

Unity writes NUnit XML to each `-testResults` path. Check the root `test-suite` totals (`total`, `passed`, `failed`, and `skipped`) and inspect failed `test-case` elements for messages and stack traces. A nonzero Unity process exit code or missing XML is also a failed run.
