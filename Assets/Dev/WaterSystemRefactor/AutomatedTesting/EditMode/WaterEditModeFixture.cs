using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class WaterEditModeFixture
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

    protected TerrainTypeDef CreateTerrain(
        bool participates = true,
        float drainageMultiplier = 1f,
        RendererDef renderer = null)
    {
        renderer ??= CreateProductionRenderer();
        TerrainTypeDef terrain = Track(ScriptableObject.CreateInstance<TerrainTypeDef>());
        terrain.Configure("test-terrain", participates, drainageMultiplier, renderer);
        return terrain;
    }

    protected RendererDef CreateRenderer(
        TileBase dryTile = null,
        Color? dryTint = null,
        WaterVisualBand[] bands = null)
    {
        RendererDef renderer = Track(ScriptableObject.CreateInstance<RendererDef>());
        renderer.Configure(dryTile, dryTint ?? Color.white, bands ?? Array.Empty<WaterVisualBand>());
        return renderer;
    }

    protected MapDef CreateMap(
        int width,
        int height,
        Vector2Int? origin = null,
        float[] depths = null,
        bool[] exists = null,
        bool[] initialWaterBodies = null,
        TerrainTypeDef[] terrains = null,
        int[] elevations = null)
    {
        int cellCount = width * height;
        TerrainTypeDef defaultTerrain = CreateTerrain();
        MapDef map = Track(ScriptableObject.CreateInstance<MapDef>());
        Vector2Int mapOrigin = origin ?? Vector2Int.zero;
        var cells = new MapCellDef[cellCount];

        for (int i = 0; i < cellCount; i++)
        {
            int x = i % width;
            int y = i / width;
            float depth = depths != null ? depths[i] : 0f;
            bool cellExists = exists == null || exists[i];
            bool waterBody = initialWaterBodies != null && initialWaterBodies[i];
            TerrainTypeDef terrain = terrains != null ? terrains[i] : defaultTerrain;
            int elevation = elevations != null ? elevations[i] : 0;
            cells[i] = CreateMapCell(cellExists, elevation, terrain, depth, waterBody);
        }

        SetSerializedField(map, "origin", mapOrigin);
        SetSerializedField(map, "width", width);
        SetSerializedField(map, "height", height);
        SetSerializedField(map, "cells", cells);

        return map;
    }

    protected WaterRuntimeFixture CreateRuntime(
        int width,
        int height,
        float[] depths = null,
        Vector2Int? origin = null,
        bool[] exists = null,
        bool[] waterBodies = null,
        TerrainTypeDef[] terrains = null,
        int[] elevations = null,
        WaterSimulationSettings settings = null)
    {
        MapDef map = CreateMap(width, height, origin, depths, exists, waterBodies, terrains, elevations);
        MapAccessor accessor = new MapAccessor(map);
        WaterState state = new WaterState(accessor);
        WaterPhysicsBarrier barriers = new WaterPhysicsBarrier();
        Assert.That(barriers.InitializeForSimulation(state.GridWidth, state.GridHeight), Is.True);

        WaterSimulationSettings resolvedSettings = settings ?? CreateSettings();
        WaterPhysics physics = new WaterPhysics(accessor, barriers);
        physics.Initialize(state, resolvedSettings);
        return new WaterRuntimeFixture(map, accessor, state, barriers, physics, resolvedSettings);
    }

    protected static WaterSimulationSettings CreateSettings()
    {
        return new WaterSimulationSettings
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

    protected static WaterStormProfile CreateProfile(
        WaterSimulationSettings settings,
        WaterSourceSpec[] sources = null,
        string profileName = "test-profile")
    {
        WaterStormProfile profile = new WaterStormProfile();
        SetSerializedField(profile, "profileName", profileName);
        SetSerializedField(profile, "simulationSettings", settings);
        SetSerializedField(profile, "continuousSources", sources ?? Array.Empty<WaterSourceSpec>());
        return profile;
    }

    protected ScenarioDef CreateScenario(
        WaterStormProfile baseline = null,
        WaterStormProfile preliminary = null,
        WaterStormProfile crisis = null,
        WaterSourceSpec[] initialSources = null,
        bool hasPreliminaryFlooding = false,
        WaterPreliminaryFloodingConfig preliminaryFlooding = null)
    {
        WaterSimulationSettings settings = CreateSettings();
        ScenarioDef scenario = Track(ScriptableObject.CreateInstance<ScenarioDef>());
        SetSerializedField(scenario, "baselineProfile", baseline ?? CreateProfile(settings));
        SetSerializedField(scenario, "preliminaryProfile", preliminary ?? CreateProfile(settings));
        SetSerializedField(scenario, "crisisProfile", crisis ?? CreateProfile(settings));
        SetSerializedField(scenario, "initialSources", initialSources ?? Array.Empty<WaterSourceSpec>());
        SetSerializedField(scenario, "hasPreliminaryFlooding", hasPreliminaryFlooding);
        SetSerializedField(
            scenario,
            "preliminaryFlooding",
            preliminaryFlooding ?? CreatePreliminaryFlooding(1, 1f));
        return scenario;
    }

    protected static WaterPreliminaryFloodingConfig CreatePreliminaryFlooding(int threshold, float duration)
    {
        WaterPreliminaryFloodingConfig configuration = new WaterPreliminaryFloodingConfig();
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

    private RendererDef CreateProductionRenderer()
    {
        Tile dryTile = Track(ScriptableObject.CreateInstance<Tile>());
        Tile floodedTile = Track(ScriptableObject.CreateInstance<Tile>());
        return CreateRenderer(
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

public sealed class WaterRuntimeFixture
{
    public WaterRuntimeFixture(
        MapDef map,
        MapAccessor accessor,
        WaterState state,
        WaterPhysicsBarrier barriers,
        WaterPhysics physics,
        WaterSimulationSettings settings)
    {
        Map = map;
        Accessor = accessor;
        State = state;
        Barriers = barriers;
        Physics = physics;
        Settings = settings;
    }

    public MapDef Map { get; }
    public MapAccessor Accessor { get; }
    public WaterState State { get; }
    public WaterPhysicsBarrier Barriers { get; }
    public WaterPhysics Physics { get; }
    public WaterSimulationSettings Settings { get; }
}

internal static class WaterAssert
{
    public static void Multiple(Action assertions)
    {
        assertions();
    }
}
