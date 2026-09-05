using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class Dev_WaterEditModeFixture
{
    protected const float Tolerance = 0.0001f;

    private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

    // Fixture teardown

    [TearDown]
    public void TearDownFixture()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            if (_createdObjects[i] != null)
                UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
        }

        _createdObjects.Clear();
    }

    // Fixture builders

    protected Dev_TerrainTypeDef CreateTerrain(
        bool participates = true,
        float drainageMultiplier = 1f,
        Dev_RendererDef renderer = null)
    {
        Dev_TerrainTypeDef terrain = Track(ScriptableObject.CreateInstance<Dev_TerrainTypeDef>());
        terrain.Configure("test-terrain", participates, drainageMultiplier, renderer);
        return terrain;
    }

    protected Dev_RendererDef CreateRenderer(
        TileBase dryTile = null,
        Color? dryTint = null,
        Dev_WaterVisualBand[] bands = null)
    {
        Dev_RendererDef renderer = Track(ScriptableObject.CreateInstance<Dev_RendererDef>());
        renderer.Configure(dryTile, dryTint ?? Color.white, bands ?? Array.Empty<Dev_WaterVisualBand>());
        return renderer;
    }

    protected Dev_MapDef CreateMap(
        int width,
        int height,
        Vector2Int? origin = null,
        float[] depths = null,
        bool[] exists = null,
        bool[] initialWaterBodies = null,
        Dev_TerrainTypeDef[] terrains = null,
        int[] elevations = null)
    {
        int cellCount = width * height;
        Dev_TerrainTypeDef defaultTerrain = CreateTerrain();
        Dev_MapDef map = Track(ScriptableObject.CreateInstance<Dev_MapDef>());
        map.Configure(origin ?? Vector2Int.zero, width, height);

        for (int i = 0; i < cellCount; i++)
        {
            int x = i % width;
            int y = i / width;
            float depth = depths != null ? depths[i] : 0f;
            bool cellExists = exists == null || exists[i];
            bool waterBody = initialWaterBodies != null && initialWaterBodies[i];
            Dev_TerrainTypeDef terrain = terrains != null ? terrains[i] : defaultTerrain;
            int elevation = elevations != null ? elevations[i] : 0;
            map.TryConfigureCell(map.Origin + new Vector2Int(x, y), elevation, terrain, depth, waterBody, cellExists);
        }

        return map;
    }

    protected Dev_WaterRuntimeFixture CreateRuntime(
        int width,
        int height,
        float[] depths = null,
        Vector2Int? origin = null,
        bool[] exists = null,
        bool[] waterBodies = null,
        Dev_TerrainTypeDef[] terrains = null,
        int[] elevations = null,
        Dev_WaterSimulationSettings settings = null)
    {
        Dev_MapDef map = CreateMap(width, height, origin, depths, exists, waterBodies, terrains, elevations);
        Dev_MapAccessor accessor = new Dev_MapAccessor(map);
        Dev_WaterState state = new Dev_WaterState(accessor);
        Dev_WaterPhysicsBarrier barriers = new Dev_WaterPhysicsBarrier();
        Assert.That(barriers.InitializeForSimulation(state.GridWidth, state.GridHeight), Is.True);

        Dev_WaterSimulationSettings resolvedSettings = settings ?? CreateSettings();
        Dev_WaterPhysics physics = new Dev_WaterPhysics(accessor, barriers);
        physics.Initialize(state, resolvedSettings);
        return new Dev_WaterRuntimeFixture(map, accessor, state, barriers, physics, resolvedSettings);
    }

    protected static Dev_WaterSimulationSettings CreateSettings()
    {
        return new Dev_WaterSimulationSettings
        {
            dx = 1f,
            dy = 1f,
            dt = 0.25f,
            gravity = 0f,
            friction = 0f,
            maxWaterDepth = 100f,
            useBoundaryWalls = true,
            useSpreadGating = false,
            spreadInterval = 1f,
            spreadLayersPerTick = 1,
            expandFromWaterThreshold = 0.001f,
            expandOnceImmediatelyOnStart = false,
            baseDrainageDepthPerSecond = 0f,
            windForceScale = 0f,
            overtopDepthForFullFlow = 1f
        };
    }

    protected static Dev_WaterStormProfile CreateProfile(
        Dev_WaterSimulationSettings settings,
        Dev_WaterSourceSpec[] sources = null,
        string profileName = "test-profile")
    {
        Dev_WaterStormProfile profile = new Dev_WaterStormProfile();
        SetSerializedField(profile, "profileName", profileName);
        SetSerializedField(profile, "simulationSettings", settings);
        SetSerializedField(profile, "continuousSources", sources ?? Array.Empty<Dev_WaterSourceSpec>());
        return profile;
    }

    protected Dev_ScenarioDef CreateScenario(
        Dev_WaterStormProfile baseline = null,
        Dev_WaterStormProfile preliminary = null,
        Dev_WaterStormProfile crisis = null,
        Dev_WaterSourceSpec[] initialSources = null,
        bool hasPreliminaryFlooding = false,
        Dev_WaterPreliminaryFloodingConfig preliminaryFlooding = null)
    {
        Dev_WaterSimulationSettings settings = CreateSettings();
        Dev_ScenarioDef scenario = Track(ScriptableObject.CreateInstance<Dev_ScenarioDef>());
        SetSerializedField(scenario, "baselineProfile", baseline ?? CreateProfile(settings));
        SetSerializedField(scenario, "preliminaryProfile", preliminary ?? CreateProfile(settings));
        SetSerializedField(scenario, "crisisProfile", crisis ?? CreateProfile(settings));
        SetSerializedField(scenario, "initialSources", initialSources ?? Array.Empty<Dev_WaterSourceSpec>());
        SetSerializedField(scenario, "hasPreliminaryFlooding", hasPreliminaryFlooding);
        SetSerializedField(
            scenario,
            "preliminaryFlooding",
            preliminaryFlooding ?? CreatePreliminaryFlooding(1, 1f));
        return scenario;
    }

    protected static Dev_WaterPreliminaryFloodingConfig CreatePreliminaryFlooding(int threshold, float duration)
    {
        Dev_WaterPreliminaryFloodingConfig configuration = new Dev_WaterPreliminaryFloodingConfig();
        SetSerializedField(configuration, "completedPreparationTurnThreshold", threshold);
        SetSerializedField(configuration, "simulatedDuration", duration);
        return configuration;
    }

    protected T Track<T>(T instance) where T : UnityEngine.Object
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

public sealed class Dev_WaterRuntimeFixture
{
    public Dev_WaterRuntimeFixture(
        Dev_MapDef map,
        Dev_MapAccessor accessor,
        Dev_WaterState state,
        Dev_WaterPhysicsBarrier barriers,
        Dev_WaterPhysics physics,
        Dev_WaterSimulationSettings settings)
    {
        Map = map;
        Accessor = accessor;
        State = state;
        Barriers = barriers;
        Physics = physics;
        Settings = settings;
    }

    public Dev_MapDef Map { get; }
    public Dev_MapAccessor Accessor { get; }
    public Dev_WaterState State { get; }
    public Dev_WaterPhysicsBarrier Barriers { get; }
    public Dev_WaterPhysics Physics { get; }
    public Dev_WaterSimulationSettings Settings { get; }
}

internal static class Dev_WaterAssert
{
    public static void Multiple(Action assertions)
    {
        assertions();
    }
}
