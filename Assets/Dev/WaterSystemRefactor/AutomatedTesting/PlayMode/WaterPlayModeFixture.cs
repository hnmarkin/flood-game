using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public abstract class WaterPlayModeFixture
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

    protected WaterControllerFixture CreateControllerFixture(
        WaterConfigurationMode mode = WaterConfigurationMode.DevDefaultsWithWarnings,
        bool includeScenario = true,
        bool includeModifierProvider = true,
        bool initialize = true,
        bool addProjectionController = false,
        bool includeCrisisProfile = true,
        WaterSourceSpec[] continuousSources = null,
        float[] initialDepths = null)
    {
        GameObject root = Track(new GameObject("WaterControllerFixture"));
        root.SetActive(false);

        WaterTestModifierProvider provider = root.AddComponent<WaterTestModifierProvider>();
        WaterController controller = root.AddComponent<WaterController>();
        WaterLifecycleCoordinator coordinator = root.AddComponent<WaterLifecycleCoordinator>();

        TerrainTypeDef terrain = CreateTerrain(CreateProductionRenderer());
        MapDef map = CreateMap(2, 1, terrain, initialDepths ?? new[] { 0f, 0f });
        ScenarioDef scenario = includeScenario
            ? CreateScenario(includeCrisisProfile, continuousSources)
            : null;

        SetSerializedField(controller, "mapDef", map);
        SetSerializedField(controller, "scenarioDef", scenario);
        SetSerializedField(controller, "modifierProviderBehaviour", includeModifierProvider ? provider : null);
        SetSerializedField(controller, "configurationMode", mode);
        SetSerializedField(controller, "initializeOnStart", false);
        SetSerializedField(controller, "startOnPlay", false);
        SetSerializedField(controller, "stepMode", WaterStepMode.Manual);
        SetSerializedField(controller, "spaceKeyStepsWhenManual", false);
        SetSerializedField(coordinator, "waterController", controller);

        ProjectionController projectionController = null;
        if (addProjectionController)
        {
            projectionController = root.AddComponent<ProjectionController>();
            SetSerializedField(projectionController, "waterController", controller);
            SetSerializedField(projectionController, "forecastSimulatedDuration", 1f);
        }

        root.SetActive(true);

        WaterControllerFixture fixture = new WaterControllerFixture(
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

    protected WaterRenderingFixture CreateRenderingFixture(
        float initialDepth,
        bool includeNullTilemap = false,
        bool rendererDefinitionAssigned = true)
    {
        GameObject root = Track(new GameObject("WaterRenderingFixture"));
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

        RendererDef rendererDefinition = null;
        if (rendererDefinitionAssigned)
        {
            rendererDefinition = Track(ScriptableObject.CreateInstance<RendererDef>());
            rendererDefinition.Configure(
                dryTile,
                dryTint,
                new[]
                {
                    new WaterVisualBand
                    {
                        minimumDepth = 0.001f,
                        maximumDepth = 1f,
                        tile = shallowTile,
                        tint = shallowTint
                    },
                    new WaterVisualBand
                    {
                        minimumDepth = 1.0001f,
                        maximumDepth = 1000f,
                        tile = deepTile,
                        tint = deepTint
                    }
                });
        }

        TerrainTypeDef terrain = CreateTerrain(rendererDefinition, rendererDefinitionAssigned);
        MapDef map = CreateMap(2, 1, terrain, new[] { initialDepth, 0f });
        MapAccessor accessor = new MapAccessor(map);
        WaterState state = new WaterState(accessor);
        WaterRenderer renderer = root.AddComponent<WaterRenderer>();
        SetSerializedField(
            renderer,
            "tilemaps",
            includeNullTilemap ? new Tilemap[] { tilemap, null } : new[] { tilemap });

        return new WaterRenderingFixture(
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
        WaterController controller,
        WaterConfigurationMode mode)
    {
        SetSerializedField(controller, "configurationMode", mode);
    }

    private TerrainTypeDef CreateTerrain(RendererDef renderer = null, bool requireRenderer = true)
    {
        if (renderer == null && requireRenderer)
            renderer = CreateProductionRenderer();

        TerrainTypeDef terrain = Track(ScriptableObject.CreateInstance<TerrainTypeDef>());
        terrain.Configure("playmode-test", true, 1f, renderer);
        return terrain;
    }

    private MapDef CreateMap(int width, int height, TerrainTypeDef terrain, float[] depths)
    {
        MapDef map = Track(ScriptableObject.CreateInstance<MapDef>());
        var cells = new MapCellDef[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float depth = depths[index];
                cells[index] = CreateMapCell(true, 0, terrain, depth, depth > 0f);
            }
        }

        SetSerializedField(map, "origin", Vector2Int.zero);
        SetSerializedField(map, "width", width);
        SetSerializedField(map, "height", height);
        SetSerializedField(map, "cells", cells);

        return map;
    }

    private ScenarioDef CreateScenario(bool includeCrisisProfile, WaterSourceSpec[] continuousSources)
    {
        WaterSimulationSettings settings = CreateSettings();
        WaterStormProfile baseline = CreateProfile(settings, continuousSources);
        WaterStormProfile preliminary = CreateProfile(settings, continuousSources);
        WaterStormProfile crisis = includeCrisisProfile ? CreateProfile(settings, continuousSources) : null;
        WaterPreliminaryFloodingConfig flooding = new WaterPreliminaryFloodingConfig();
        SetSerializedField(flooding, "completedPreparationTurnThreshold", 2);
        SetSerializedField(flooding, "simulatedDuration", 0.5f);

        ScenarioDef scenario = Track(ScriptableObject.CreateInstance<ScenarioDef>());
        SetSerializedField(scenario, "baselineProfile", baseline);
        SetSerializedField(scenario, "preliminaryProfile", preliminary);
        SetSerializedField(scenario, "crisisProfile", crisis);
        SetSerializedField(scenario, "initialSources", Array.Empty<WaterSourceSpec>());
        SetSerializedField(scenario, "hasPreliminaryFlooding", true);
        SetSerializedField(scenario, "preliminaryFlooding", flooding);
        return scenario;
    }

    private static WaterStormProfile CreateProfile(
        WaterSimulationSettings settings,
        WaterSourceSpec[] continuousSources)
    {
        WaterStormProfile profile = new WaterStormProfile();
        SetSerializedField(profile, "profileName", "playmode-profile");
        SetSerializedField(profile, "simulationSettings", settings.Clone());
        SetSerializedField(
            profile,
            "continuousSources",
            continuousSources ?? Array.Empty<WaterSourceSpec>());
        return profile;
    }

    private static WaterSimulationSettings CreateSettings()
    {
        return new WaterSimulationSettings
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

    private RendererDef CreateProductionRenderer()
    {
        Tile dryTile = Track(ScriptableObject.CreateInstance<Tile>());
        Tile floodedTile = Track(ScriptableObject.CreateInstance<Tile>());
        dryTile.flags = TileFlags.None;
        floodedTile.flags = TileFlags.None;

        RendererDef renderer = Track(ScriptableObject.CreateInstance<RendererDef>());
        renderer.Configure(
            dryTile,
            Color.white,
            new[]
            {
                new WaterVisualBand
                {
                    minimumDepth = 0.001f,
                    maximumDepth = 1000f,
                    tile = floodedTile,
                    tint = Color.white
                }
            });
        return renderer;
    }

    private static MapCellDef CreateMapCell(
        bool exists,
        int elevation,
        TerrainTypeDef terrain,
        float initialWaterDepth,
        bool initialWaterBody)
    {
        var cell = new MapCellDef();
        SetSerializedField(cell, "exists", exists);
        SetSerializedField(cell, "elevation", elevation);
        SetSerializedField(cell, "terrain", terrain);
        SetSerializedField(cell, "initialWaterDepth", initialWaterDepth);
        SetSerializedField(cell, "isInitialWaterBody", initialWaterBody);
        return cell;
    }
}

public sealed class WaterControllerFixture
{
    public WaterControllerFixture(
        GameObject root,
        MapDef map,
        ScenarioDef scenario,
        WaterTestModifierProvider provider,
        WaterController controller,
        WaterLifecycleCoordinator coordinator,
        ProjectionController projectionController)
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
    public MapDef Map { get; }
    public ScenarioDef Scenario { get; }
    public WaterTestModifierProvider Provider { get; }
    public WaterController Controller { get; }
    public WaterLifecycleCoordinator Coordinator { get; }
    public ProjectionController ProjectionController { get; }
    public bool InitializationResult { get; set; }
}

public sealed class WaterRenderingFixture
{
    public WaterRenderingFixture(
        GameObject root,
        Tilemap tilemap,
        Tile dryTile,
        Tile shallowTile,
        Tile deepTile,
        Color dryTint,
        Color shallowTint,
        Color deepTint,
        MapDef map,
        MapAccessor accessor,
        WaterState state,
        WaterRenderer renderer)
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
    public MapDef Map { get; }
    public MapAccessor Accessor { get; }
    public WaterState State { get; }
    public WaterRenderer Renderer { get; }
}

public sealed class WaterTestModifierProvider : MonoBehaviour, IWaterModifierProvider
{
    public int FailOnCall { get; set; }
    public int CallCount { get; private set; }
    public WaterModifierSnapshot Modifiers { get; set; } = WaterModifierSnapshot.Defaults();
    public Action OnResolve { get; set; }

    public bool TryGetResolvedWaterModifiers(out WaterModifierSnapshot modifiers, out string error)
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

internal static class WaterAssert
{
    public static void Multiple(Action assertions)
    {
        assertions();
    }
}
