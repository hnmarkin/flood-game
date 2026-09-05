using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class Dev_WaterPhysicsTests
{
    private readonly List<Object> _createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            if (_createdObjects[i] != null)
                Object.DestroyImmediate(_createdObjects[i]);
        }

        _createdObjects.Clear();
    }

    [Test]
    public void ModifierSnapshot_RejectsNonFiniteValues()
    {
        Dev_WaterModifierSnapshot modifiers = Dev_WaterModifierSnapshot.Defaults();
        modifiers.RainfallRate = float.NaN;

        Assert.That(modifiers.IsValid(out string error), Is.False);
        Assert.That(error, Does.Contain("finite"));
    }

    [Test]
    public void Barrier_RejectsZeroHeight()
    {
        Dev_WaterPhysicsBarrier barriers = new Dev_WaterPhysicsBarrier();
        Assert.That(barriers.InitializeForSimulation(4, 4), Is.True);

        Assert.That(barriers.TrySetBarrierX(2, 2, 0f), Is.False);
        Assert.That(barriers.IsBlockedX(2, 2), Is.False);
    }

    [Test]
    public void MapValidation_RequiresCompleteCellsAndTerrainParticipationIsHonored()
    {
        Dev_MapDef map = CreateMap(2, 2, 0f, true);
        Assert.That(map.IsValidForProduction(out string validError), Is.True, validError);

        Dev_TerrainTypeDef nonSimulatingTerrain = CreateTerrain(false);
        Assert.That(map.TryConfigureCell(new Vector2Int(1, 1), 0, nonSimulatingTerrain, 0f, false), Is.True);
        Assert.That(map.IsValidForProduction(out validError), Is.True, validError);

        Dev_MapAccessor accessor = new Dev_MapAccessor(map);
        Assert.That(accessor.IsSimulationCell(2, 2), Is.True);
        Assert.That(accessor.IsSimulationCell(2, 1), Is.True);
        Assert.That(accessor.IsSimulationCell(1, 1), Is.False);
    }

    [Test]
    public void State_SetWaterDepth_ActivatesNewlyWetCell()
    {
        PhysicsFixture fixture = CreateFixture(2, 1, 0f);

        Assert.That(fixture.State.TrySetWaterDepth(new Vector2Int(1, 0), 0.5f), Is.True);
        Assert.That(fixture.State.Active[1, 1], Is.True);
        Assert.That(fixture.State.GetWaterDepth(new Vector2Int(1, 0)), Is.EqualTo(0.5f));
    }

    [Test]
    public void Physics_SpreadGateDoesNotLeakFlowIntoInactiveCells()
    {
        PhysicsFixture fixture = CreateFixture(2, 1, 0f, new[] { 1f, 0f });
        fixture.Settings.gravity = 9.81f;
        fixture.Settings.expandFromWaterThreshold = 0.1f;
        fixture.Settings.expandOnceImmediatelyOnStart = false;
        fixture.Settings.useSpreadGating = true;
        fixture.Physics.Initialize(fixture.State, fixture.Settings);
        fixture.Physics.InitializeActiveRegion();

        fixture.Physics.Step(null, Dev_WaterModifierSnapshot.Defaults(), 0.25f);

        Assert.That(fixture.State.Water[1, 1], Is.EqualTo(1f).Within(0.0001f));
        Assert.That(fixture.State.Water[2, 1], Is.EqualTo(0f).Within(0.0001f));
        Assert.That(fixture.State.FlowX[2, 1], Is.EqualTo(0f));
    }

    [Test]
    public void InitialEdgeSource_DoesNotDoubleCountOneByOneMap()
    {
        PhysicsFixture fixture = CreateFixture(1, 1, 0f);
        Dev_WaterSourceSpec source = new Dev_WaterSourceSpec
        {
            kind = Dev_WaterSourceKind.Edges,
            depth = 3f,
            scaleByExternalWaterLoad = false
        };

        fixture.Physics.ApplyInitialSources(new[] { source }, Dev_WaterModifierSnapshot.Defaults());

        Assert.That(fixture.State.Water[1, 1], Is.EqualTo(3f).Within(0.0001f));
    }

    [Test]
    public void BoundarySource_InjectsConfiguredDepthPerSecond()
    {
        PhysicsFixture fixture = CreateFixture(3, 2, 0f);
        fixture.Settings.gravity = 0f;
        fixture.Settings.dt = 1f;
        fixture.Settings.expandFromWaterThreshold = 100f;
        fixture.Settings.northBoundary.mode = Dev_WaterBoundaryMode.Source;
        fixture.Settings.northBoundary.sourceDepthPerSecond = 2f;
        fixture.Physics.Initialize(fixture.State, fixture.Settings);

        fixture.Physics.Step(null, Dev_WaterModifierSnapshot.Defaults(), 1f);

        for (int x = 1; x <= 3; x++)
        {
            Assert.That(fixture.State.Water[x, 2], Is.EqualTo(2f).Within(0.0001f));
            Assert.That(fixture.State.Water[x, 1], Is.EqualTo(0f).Within(0.0001f));
        }
    }

    [Test]
    public void BoundarySink_RemovesAllEdgeWater()
    {
        PhysicsFixture fixture = CreateFixture(3, 2, 1f);
        fixture.Settings.useSpreadGating = false;
        fixture.Settings.gravity = 0f;
        fixture.Settings.northBoundary.mode = Dev_WaterBoundaryMode.Sink;
        fixture.Physics.Initialize(fixture.State, fixture.Settings);

        fixture.Physics.Step(null, Dev_WaterModifierSnapshot.Defaults(), 0.25f);

        for (int x = 1; x <= 3; x++)
        {
            Assert.That(fixture.State.Water[x, 2], Is.EqualTo(0f).Within(0.0001f));
            Assert.That(fixture.State.Water[x, 1], Is.EqualTo(1f).Within(0.0001f));
        }
    }

    [Test]
    public void BoundaryWallSeepage_DrainsOnlyConfiguredRate()
    {
        PhysicsFixture fixture = CreateFixture(3, 2, 1f);
        fixture.Settings.useSpreadGating = false;
        fixture.Settings.gravity = 0f;
        fixture.Settings.northBoundary.seepageDepthPerSecond = 2f;
        fixture.Physics.Initialize(fixture.State, fixture.Settings);

        fixture.Physics.Step(null, Dev_WaterModifierSnapshot.Defaults(), 0.25f);

        for (int x = 1; x <= 3; x++)
        {
            Assert.That(fixture.State.Water[x, 2], Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(fixture.State.Water[x, 1], Is.EqualTo(1f).Within(0.0001f));
        }
    }

    [Test]
    public void ProjectionInitialization_CanPreserveSpreadTimer()
    {
        PhysicsFixture fixture = CreateFixture(2, 1, 0f, new[] { 1f, 0f });
        fixture.Settings.useSpreadGating = true;
        fixture.Settings.expandFromWaterThreshold = 0.1f;
        fixture.Settings.spreadInterval = 1f;
        fixture.Settings.expandOnceImmediatelyOnStart = false;
        fixture.Physics.Initialize(fixture.State, fixture.Settings);
        fixture.Physics.InitializeActiveRegion();

        Dev_WaterState projectionState = new Dev_WaterState(new Dev_MapAccessor(fixture.Map));
        Dev_WaterPhysics projectionPhysics = new Dev_WaterPhysics(
            new Dev_MapAccessor(fixture.Map),
            CreateBarriers(projectionState));

        projectionPhysics.Initialize(projectionState, fixture.Settings);
        projectionPhysics.InitializeActiveRegion();

        // The overload is exercised through a real state below; this assertion documents
        // that a supplied timer is retained instead of unconditionally reset.
        projectionPhysics.InitializeProjection(projectionState, fixture.Settings, 0.75f);
        projectionPhysics.TickSpreadGate(0.1f);
        Assert.That(projectionState.Active[2, 1], Is.False);
        projectionPhysics.TickSpreadGate(0.2f);
        Assert.That(projectionState.Active[2, 1], Is.True);
    }

    [Test]
    public void RendererBand_AtThresholdHasNoGap()
    {
        Dev_RendererDef renderer = CreateRenderer();
        renderer.Configure(null, Color.black, new[]
        {
            new Dev_WaterVisualBand { minimumDepth = 0.001f, maximumDepth = 2f, tint = Color.green },
            new Dev_WaterVisualBand { minimumDepth = 2f, maximumDepth = 1000f, tint = Color.blue }
        });

        Assert.That(renderer.ResolveTint(2.0005f), Is.EqualTo(Color.blue));
    }

    private PhysicsFixture CreateFixture(int width, int height, float initialWater, float[] depths = null)
    {
        Dev_MapDef map = CreateMap(width, height, initialWater, true, depths);
        Dev_MapAccessor accessor = new Dev_MapAccessor(map);
        Dev_WaterState state = new Dev_WaterState(accessor);
        Dev_WaterPhysicsBarrier barriers = CreateBarriers(state);

        Dev_WaterSimulationSettings settings = new Dev_WaterSimulationSettings
        {
            gravity = 0f,
            friction = 0f,
            maxWaterDepth = 100f,
            useSpreadGating = false,
            expandOnceImmediatelyOnStart = false,
            baseDrainageDepthPerSecond = 0f,
            windForceScale = 0f
        };
        Dev_WaterPhysics physics = new Dev_WaterPhysics(accessor, barriers);
        physics.Initialize(state, settings);
        return new PhysicsFixture(map, state, physics, settings);
    }

    private Dev_WaterPhysicsBarrier CreateBarriers(Dev_WaterState state)
    {
        Dev_WaterPhysicsBarrier barriers = new Dev_WaterPhysicsBarrier();
        Assert.That(barriers.InitializeForSimulation(state.GridWidth, state.GridHeight), Is.True);
        return barriers;
    }

    private Dev_MapDef CreateMap(int width, int height, float initialWater, bool participates, float[] depths = null)
    {
        Dev_MapDef map = ScriptableObject.CreateInstance<Dev_MapDef>();
        Dev_TerrainTypeDef terrain = CreateTerrain(participates);
        map.Configure(Vector2Int.zero, width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float depth = depths != null ? depths[index] : initialWater;
                map.TryConfigureCell(new Vector2Int(x, y), 0, terrain, depth, depth > 0f);
            }
        }

        _createdObjects.Add(map);
        return map;
    }

    private Dev_TerrainTypeDef CreateTerrain(bool participates)
    {
        Dev_TerrainTypeDef terrain = ScriptableObject.CreateInstance<Dev_TerrainTypeDef>();
        terrain.Configure("test", participates, 1f, null);
        _createdObjects.Add(terrain);
        return terrain;
    }

    private Dev_RendererDef CreateRenderer()
    {
        Dev_RendererDef renderer = ScriptableObject.CreateInstance<Dev_RendererDef>();
        _createdObjects.Add(renderer);
        return renderer;
    }

    private sealed class PhysicsFixture
    {
        public PhysicsFixture(Dev_MapDef map, Dev_WaterState state, Dev_WaterPhysics physics, Dev_WaterSimulationSettings settings)
        {
            Map = map;
            State = state;
            Physics = physics;
            Settings = settings;
        }

        public Dev_MapDef Map { get; }
        public Dev_WaterState State { get; }
        public Dev_WaterPhysics Physics { get; }
        public Dev_WaterSimulationSettings Settings { get; }
    }
}
