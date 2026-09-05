using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public abstract class Dev_WaterPlayModeFixture
{
    protected const float Tolerance = 0.0001f;

    private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

    // Fixture teardown

    [UnityTearDown]
    public IEnumerator TearDownFixture()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            if (_createdObjects[i] != null)
                UnityEngine.Object.Destroy(_createdObjects[i]);
        }

        _createdObjects.Clear();
        yield return null;
    }

    // Fixture builders

    protected Dev_WaterControllerFixture CreateControllerFixture(
        Dev_WaterConfigurationMode mode = Dev_WaterConfigurationMode.DevDefaultsWithWarnings,
        bool includeScenario = true,
        bool includeModifierProvider = true,
        bool initialize = true,
        bool addProjectionController = false,
        bool includeCrisisProfile = true,
        Dev_WaterSourceSpec[] continuousSources = null,
        float[] initialDepths = null)
    {
        GameObject root = Track(new GameObject("Dev_WaterControllerFixture"));
        root.SetActive(false);

        Dev_WaterTestModifierProvider provider = root.AddComponent<Dev_WaterTestModifierProvider>();
        Dev_WaterController controller = root.AddComponent<Dev_WaterController>();
        Dev_WaterLifecycleCoordinator coordinator = root.AddComponent<Dev_WaterLifecycleCoordinator>();

        Dev_TerrainTypeDef terrain = CreateTerrain();
        Dev_MapDef map = CreateMap(2, 1, terrain, initialDepths ?? new[] { 0f, 0f });
        Dev_ScenarioDef scenario = includeScenario
            ? CreateScenario(includeCrisisProfile, continuousSources)
            : null;

        SetSerializedField(controller, "mapDef", map);
        SetSerializedField(controller, "scenarioDef", scenario);
        SetSerializedField(controller, "modifierProviderBehaviour", includeModifierProvider ? provider : null);
        SetSerializedField(controller, "configurationMode", mode);
        SetSerializedField(controller, "initializeOnStart", false);
        SetSerializedField(controller, "startOnPlay", false);
        SetSerializedField(controller, "stepMode", Dev_WaterStepMode.Manual);
        SetSerializedField(controller, "spaceKeyStepsWhenManual", false);
        SetSerializedField(coordinator, "waterController", controller);

        Dev_ProjectionController projectionController = null;
        if (addProjectionController)
        {
            projectionController = root.AddComponent<Dev_ProjectionController>();
            SetSerializedField(projectionController, "waterController", controller);
            SetSerializedField(projectionController, "forecastSimulatedDuration", 1f);
        }

        root.SetActive(true);

        Dev_WaterControllerFixture fixture = new Dev_WaterControllerFixture(
            root,
            map,
            scenario,
            provider,
            controller,
            coordinator,
            projectionController);

        if (initialize)
            fixture.InitializationResult = controller.InitializeRuntimeState();

        return fixture;
    }

    protected Dev_WaterRenderingFixture CreateRenderingFixture(
        float initialDepth,
        bool includeNullTilemap = false,
        bool rendererDefinitionAssigned = true)
    {
        GameObject root = Track(new GameObject("Dev_WaterRenderingFixture"));
        root.AddComponent<Grid>();

        GameObject tilemapObject = new GameObject("WaterTilemap");
        tilemapObject.transform.SetParent(root.transform);
        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemapObject.AddComponent<TilemapRenderer>();

        Tile dryTile = Track(ScriptableObject.CreateInstance<Tile>());
        Tile shallowTile = Track(ScriptableObject.CreateInstance<Tile>());
        Tile deepTile = Track(ScriptableObject.CreateInstance<Tile>());
        dryTile.flags = TileFlags.None;
        shallowTile.flags = TileFlags.None;
        deepTile.flags = TileFlags.None;
        Color dryTint = new Color(0.8f, 0.7f, 0.6f, 1f);
        Color shallowTint = new Color(0.2f, 0.6f, 0.9f, 1f);
        Color deepTint = new Color(0.05f, 0.2f, 0.5f, 1f);

        Dev_RendererDef rendererDefinition = null;
        if (rendererDefinitionAssigned)
        {
            rendererDefinition = Track(ScriptableObject.CreateInstance<Dev_RendererDef>());
            rendererDefinition.Configure(
                dryTile,
                dryTint,
                new[]
                {
                    new Dev_WaterVisualBand
                    {
                        minimumDepth = 0.001f,
                        maximumDepth = 1f,
                        tile = shallowTile,
                        tint = shallowTint
                    },
                    new Dev_WaterVisualBand
                    {
                        minimumDepth = 1.0001f,
                        maximumDepth = 1000f,
                        tile = deepTile,
                        tint = deepTint
                    }
                });
        }

        Dev_TerrainTypeDef terrain = CreateTerrain(rendererDefinition);
        Dev_MapDef map = CreateMap(2, 1, terrain, new[] { initialDepth, 0f });
        Dev_MapAccessor accessor = new Dev_MapAccessor(map);
        Dev_WaterState state = new Dev_WaterState(accessor);
        Dev_WaterRenderer renderer = root.AddComponent<Dev_WaterRenderer>();
        SetSerializedField(
            renderer,
            "tilemaps",
            includeNullTilemap ? new Tilemap[] { tilemap, null } : new[] { tilemap });

        return new Dev_WaterRenderingFixture(
            root,
            tilemap,
            dryTile,
            shallowTile,
            deepTile,
            dryTint,
            shallowTint,
            deepTint,
            map,
            accessor,
            state,
            renderer);
    }

    protected static void ConfigureControllerMode(
        Dev_WaterController controller,
        Dev_WaterConfigurationMode mode)
    {
        SetSerializedField(controller, "configurationMode", mode);
    }

    private Dev_TerrainTypeDef CreateTerrain(Dev_RendererDef renderer = null)
    {
        Dev_TerrainTypeDef terrain = Track(ScriptableObject.CreateInstance<Dev_TerrainTypeDef>());
        terrain.Configure("playmode-test", true, 1f, renderer);
        return terrain;
    }

    private Dev_MapDef CreateMap(int width, int height, Dev_TerrainTypeDef terrain, float[] depths)
    {
        Dev_MapDef map = Track(ScriptableObject.CreateInstance<Dev_MapDef>());
        map.Configure(Vector2Int.zero, width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float depth = depths[index];
                map.TryConfigureCell(new Vector2Int(x, y), 0, terrain, depth, depth > 0f);
            }
        }

        return map;
    }

    private Dev_ScenarioDef CreateScenario(bool includeCrisisProfile, Dev_WaterSourceSpec[] continuousSources)
    {
        Dev_WaterSimulationSettings settings = CreateSettings();
        Dev_WaterStormProfile baseline = CreateProfile(settings, continuousSources);
        Dev_WaterStormProfile preliminary = CreateProfile(settings, continuousSources);
        Dev_WaterStormProfile crisis = includeCrisisProfile ? CreateProfile(settings, continuousSources) : null;
        Dev_WaterPreliminaryFloodingConfig flooding = new Dev_WaterPreliminaryFloodingConfig();
        SetSerializedField(flooding, "completedPreparationTurnThreshold", 2);
        SetSerializedField(flooding, "simulatedDuration", 0.5f);

        Dev_ScenarioDef scenario = Track(ScriptableObject.CreateInstance<Dev_ScenarioDef>());
        SetSerializedField(scenario, "baselineProfile", baseline);
        SetSerializedField(scenario, "preliminaryProfile", preliminary);
        SetSerializedField(scenario, "crisisProfile", crisis);
        SetSerializedField(scenario, "initialSources", Array.Empty<Dev_WaterSourceSpec>());
        SetSerializedField(scenario, "hasPreliminaryFlooding", true);
        SetSerializedField(scenario, "preliminaryFlooding", flooding);
        return scenario;
    }

    private static Dev_WaterStormProfile CreateProfile(
        Dev_WaterSimulationSettings settings,
        Dev_WaterSourceSpec[] continuousSources)
    {
        Dev_WaterStormProfile profile = new Dev_WaterStormProfile();
        SetSerializedField(profile, "profileName", "playmode-profile");
        SetSerializedField(profile, "simulationSettings", settings.Clone());
        SetSerializedField(
            profile,
            "continuousSources",
            continuousSources ?? Array.Empty<Dev_WaterSourceSpec>());
        return profile;
    }

    private static Dev_WaterSimulationSettings CreateSettings()
    {
        return new Dev_WaterSimulationSettings
        {
            dt = 0.25f,
            gravity = 0f,
            friction = 0f,
            maxWaterDepth = 100f,
            useBoundaryWalls = true,
            useSpreadGating = false,
            expandOnceImmediatelyOnStart = false,
            baseDrainageDepthPerSecond = 0f,
            windForceScale = 0f
        };
    }

    private T Track<T>(T instance) where T : UnityEngine.Object
    {
        _createdObjects.Add(instance);
        return instance;
    }

    private static void SetSerializedField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing serialized field {target.GetType().Name}.{fieldName}");
        field.SetValue(target, value);
    }
}

public sealed class Dev_WaterControllerFixture
{
    public Dev_WaterControllerFixture(
        GameObject root,
        Dev_MapDef map,
        Dev_ScenarioDef scenario,
        Dev_WaterTestModifierProvider provider,
        Dev_WaterController controller,
        Dev_WaterLifecycleCoordinator coordinator,
        Dev_ProjectionController projectionController)
    {
        Root = root;
        Map = map;
        Scenario = scenario;
        Provider = provider;
        Controller = controller;
        Coordinator = coordinator;
        ProjectionController = projectionController;
    }

    public GameObject Root { get; }
    public Dev_MapDef Map { get; }
    public Dev_ScenarioDef Scenario { get; }
    public Dev_WaterTestModifierProvider Provider { get; }
    public Dev_WaterController Controller { get; }
    public Dev_WaterLifecycleCoordinator Coordinator { get; }
    public Dev_ProjectionController ProjectionController { get; }
    public bool InitializationResult { get; set; }
}

public sealed class Dev_WaterRenderingFixture
{
    public Dev_WaterRenderingFixture(
        GameObject root,
        Tilemap tilemap,
        Tile dryTile,
        Tile shallowTile,
        Tile deepTile,
        Color dryTint,
        Color shallowTint,
        Color deepTint,
        Dev_MapDef map,
        Dev_MapAccessor accessor,
        Dev_WaterState state,
        Dev_WaterRenderer renderer)
    {
        Root = root;
        Tilemap = tilemap;
        DryTile = dryTile;
        ShallowTile = shallowTile;
        DeepTile = deepTile;
        DryTint = dryTint;
        ShallowTint = shallowTint;
        DeepTint = deepTint;
        Map = map;
        Accessor = accessor;
        State = state;
        Renderer = renderer;
    }

    public GameObject Root { get; }
    public Tilemap Tilemap { get; }
    public Tile DryTile { get; }
    public Tile ShallowTile { get; }
    public Tile DeepTile { get; }
    public Color DryTint { get; }
    public Color ShallowTint { get; }
    public Color DeepTint { get; }
    public Dev_MapDef Map { get; }
    public Dev_MapAccessor Accessor { get; }
    public Dev_WaterState State { get; }
    public Dev_WaterRenderer Renderer { get; }
}

public sealed class Dev_WaterTestModifierProvider : MonoBehaviour, IDev_WaterModifierProvider
{
    public int FailOnCall { get; set; }
    public int CallCount { get; private set; }
    public Dev_WaterModifierSnapshot Modifiers { get; set; } = Dev_WaterModifierSnapshot.Defaults();
    public Action OnResolve { get; set; }

    public bool TryGetResolvedWaterModifiers(out Dev_WaterModifierSnapshot modifiers, out string error)
    {
        CallCount++;
        Action callback = OnResolve;
        OnResolve = null;
        callback?.Invoke();

        if (FailOnCall > 0 && CallCount == FailOnCall)
        {
            modifiers = default;
            error = "intentional test failure";
            return false;
        }

        modifiers = Modifiers;
        error = null;
        return true;
    }
}

internal static class Dev_WaterAssert
{
    public static void Multiple(Action assertions)
    {
        assertions();
    }
}
