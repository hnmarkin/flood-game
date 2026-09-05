using System.Linq;
using NUnit.Framework;
using UnityEngine;

[Category("WaterUnit")]
public sealed class WaterMapStateTests : WaterEditModeFixture
{
    // Tests: map access

    [Test]
    public void MapAccessor_NonSquareMapWithNonZeroOrigin_ConvertsCoordinatesAndLooksUpSafely()
    {
        // Arrange
        Vector2Int origin = new Vector2Int(-4, 7);
        MapDef map = CreateMap(3, 2, origin, elevations: new[] { 1, 2, 3, 4, 5, 6 });
        MapAccessor accessor = new MapAccessor(map);

        // Act
        bool converted = accessor.TryTileToSim(new Vector2Int(-2, 8), out int simX, out int simY);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(converted, Is.True);
            Assert.That((simX, simY), Is.EqualTo((3, 2)));
            Assert.That(accessor.SimToTile(simX, simY), Is.EqualTo(new Vector2Int(-2, 8)));
            Assert.That(accessor.GetElevation(simX, simY), Is.EqualTo(6f).Within(Tolerance));
            Assert.That(accessor.TryGetCell(new Vector2Int(-5, 7), out _), Is.False);
            Assert.That(accessor.TryTileToSim(new Vector2Int(-1, 8), out _, out _), Is.False);
        });
    }

    [Test]
    public void MapAccessor_SparseAndNonSimulatingCells_ExcludeBothFromSimulation()
    {
        // Arrange
        TerrainTypeDef simulating = CreateTerrain();
        TerrainTypeDef nonSimulating = CreateTerrain(false);
        MapDef map = CreateMap(
            3,
            1,
            exists: new[] { true, false, true },
            terrains: new[] { simulating, simulating, nonSimulating });
        MapAccessor accessor = new MapAccessor(map);

        // Act
        bool mapIsValid = map.IsValidForProduction(out string error);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(mapIsValid, Is.False);
            Assert.That(error, Does.Contain("not marked as existing"));
            Assert.That(accessor.IsSimulationCell(1, 1), Is.True);
            Assert.That(accessor.IsSimulationCell(2, 1), Is.False);
            Assert.That(accessor.IsSimulationCell(3, 1), Is.False);
            Assert.That(accessor.TryGetCell(2, 1, out _), Is.False);
        });
    }

    [Test]
    public void MapDefinition_CompleteCells_ValidatesForProduction()
    {
        // Arrange
        MapDef map = CreateMap(2, 3, new Vector2Int(10, -3));

        // Act
        bool isValid = map.IsValidForProduction(out string error);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(isValid, Is.True, error);
            Assert.That(map.CellCount, Is.EqualTo(6));
            Assert.That(map.TryGetCell(new Vector2Int(11, -1), out MapCellDef cell), Is.True);
            Assert.That(cell, Is.Not.Null);
        });
    }

    // Tests: runtime state

    [Test]
    public void WaterState_MapSnapshot_CopiesAuthoredValuesWithoutWritingBack()
    {
        // Arrange
        Vector2Int origin = new Vector2Int(5, -2);
        MapDef map = CreateMap(2, 1, origin, new[] { 1.5f, 0.25f }, elevations: new[] { 3, 7 });
        WaterState state = new WaterState(new MapAccessor(map));

        // Act
        bool changed = state.TrySetWaterDepth(origin, 9f);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(state.Terrain[1, 1], Is.EqualTo(3f).Within(Tolerance));
            Assert.That(state.Terrain[2, 1], Is.EqualTo(7f).Within(Tolerance));
            Assert.That(state.Water[1, 1], Is.EqualTo(9f).Within(Tolerance));
            Assert.That(map.TryGetCell(origin, out MapCellDef authored), Is.True);
            Assert.That(authored.InitialWaterDepth, Is.EqualTo(1.5f).Within(Tolerance));
        });
    }

    [Test]
    public void WaterState_GuardCellsAndInvalidMutations_RemainCleanAndUntracked()
    {
        // Arrange
        WaterRuntimeFixture fixture = CreateRuntime(2, 2);

        // Act
        bool setOutside = fixture.State.TrySetWaterDepth(new Vector2Int(-1, 0), 1f);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(setOutside, Is.False);
            Assert.That(fixture.State.GetWaterDepth(new Vector2Int(-1, 0)), Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Water[0, 0], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.Water[fixture.State.GridWidth - 1, 1], Is.Zero.Within(Tolerance));
            Assert.That(fixture.State.DirtyCells, Is.Empty);
        });
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(-0.01f)]
    public void WaterState_InvalidDepth_RejectsMutation(float depth)
    {
        // Arrange
        WaterRuntimeFixture fixture = CreateRuntime(1, 1, new[] { 0.5f });

        // Act
        bool changed = fixture.State.TrySetWaterDepth(Vector2Int.zero, depth);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(changed, Is.False);
            Assert.That(fixture.State.GetWaterDepth(Vector2Int.zero), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(fixture.State.DirtyCells, Is.Empty);
        });
    }

    [Test]
    public void WaterState_ValidDepthMutation_ActivatesAndTracksOnlyChangedCell()
    {
        // Arrange
        Vector2Int origin = new Vector2Int(4, 9);
        WaterRuntimeFixture fixture = CreateRuntime(2, 1, origin: origin);

        // Act
        bool changed = fixture.State.TrySetWaterDepth(origin + Vector2Int.right, 0.5f);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(fixture.State.Active[2, 1], Is.True);
            Assert.That(fixture.State.DirtyCells.Single(), Is.EqualTo(origin + Vector2Int.right));
        });
    }

    [Test]
    public void WaterState_MarkAllExistingDirty_SkipsSparseAndNonSimulatingCells()
    {
        // Arrange
        TerrainTypeDef simulating = CreateTerrain();
        TerrainTypeDef excluded = CreateTerrain(false);
        WaterRuntimeFixture fixture = CreateRuntime(
            3,
            1,
            exists: new[] { true, false, true },
            terrains: new[] { simulating, simulating, excluded });

        // Act
        fixture.State.MarkAllExistingDirty();

        // Assert
        Assert.That(fixture.State.DirtyCells, Is.EquivalentTo(new[] { Vector2Int.zero }));
    }
}
