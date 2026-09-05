using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[Category("WaterRendering")]
public sealed class Dev_WaterRenderingPlayModeTests : Dev_WaterPlayModeFixture
{
    // Tests: initial and dirty-cell rendering

    [UnityTest]
    public IEnumerator Renderer_Initialize_RendersEveryExistingCellWithResolvedTileAndTint()
    {
        // Arrange
        Dev_WaterRenderingFixture fixture = CreateRenderingFixture(0.5f);

        // Act
        fixture.Renderer.Initialize(fixture.State, fixture.Accessor);
        yield return null;

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.Tilemap.GetTile(new Vector3Int(0, 0, 0)), Is.SameAs(fixture.ShallowTile));
            Assert.That(fixture.Tilemap.GetColor(new Vector3Int(0, 0, 0)), Is.EqualTo(fixture.ShallowTint));
            Assert.That(fixture.Tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.SameAs(fixture.DryTile));
            Assert.That(fixture.Tilemap.GetColor(new Vector3Int(1, 0, 0)), Is.EqualTo(fixture.DryTint));
            Assert.That(fixture.State.GetWaterDepth(Vector2Int.zero), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(fixture.State.DirtyCells, Is.Empty);
        });
    }

    [UnityTest]
    public IEnumerator Renderer_DirtyCellUpdate_ChangesOnlyRequestedProjectionWithoutWritingSimulation()
    {
        // Arrange
        Dev_WaterRenderingFixture fixture = CreateRenderingFixture(0f);
        fixture.Renderer.Initialize(fixture.State, fixture.Accessor);
        Assert.That(fixture.State.TrySetWaterDepth(Vector2Int.zero, 2f), Is.True);

        // Act
        fixture.Renderer.ApplyDirty();
        yield return null;

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.Tilemap.GetTile(new Vector3Int(0, 0, 0)), Is.SameAs(fixture.DeepTile));
            Assert.That(fixture.Tilemap.GetColor(new Vector3Int(0, 0, 0)), Is.EqualTo(fixture.DeepTint));
            Assert.That(fixture.Tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.SameAs(fixture.DryTile));
            Assert.That(fixture.State.GetWaterDepth(Vector2Int.zero), Is.EqualTo(2f).Within(Tolerance));
            Assert.That(fixture.State.DirtyCells, Is.Empty);
        });
    }

    [UnityTest]
    public IEnumerator Renderer_NullTilemapEntry_SkipsEntryAndRendersRemainingTilemap()
    {
        // Arrange
        Dev_WaterRenderingFixture fixture = CreateRenderingFixture(0.5f, includeNullTilemap: true);

        // Act
        fixture.Renderer.Initialize(fixture.State, fixture.Accessor);
        yield return null;

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.Tilemap.GetTile(new Vector3Int(0, 0, 0)), Is.SameAs(fixture.ShallowTile));
            Assert.That(fixture.State.GetWaterDepth(Vector2Int.zero), Is.EqualTo(0.5f).Within(Tolerance));
        });
    }

    [UnityTest]
    public IEnumerator Renderer_MissingDefinition_UsesFallbackTintWithoutMutatingWater()
    {
        // Arrange
        Dev_WaterRenderingFixture fixture = CreateRenderingFixture(0.5f, rendererDefinitionAssigned: false);
        float before = fixture.State.GetWaterDepth(Vector2Int.zero);

        // Act
        fixture.Renderer.Initialize(fixture.State, fixture.Accessor);
        yield return null;

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.Tilemap.GetTile(new Vector3Int(0, 0, 0)), Is.Null);
            Assert.That(fixture.State.GetWaterDepth(Vector2Int.zero), Is.EqualTo(before).Within(Tolerance));
            Assert.That(fixture.State.DirtyCells, Is.Empty);
        });
    }

    [UnityTest]
    public IEnumerator Renderer_NullStateOrMap_ReturnsWithoutTilemapOrStateChanges()
    {
        // Arrange
        Dev_WaterRenderingFixture fixture = CreateRenderingFixture(0.5f);

        // Act
        fixture.Renderer.Initialize(null, fixture.Accessor);
        fixture.Renderer.Initialize(fixture.State, null);
        fixture.Renderer.ApplyDirty();
        yield return null;

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(fixture.Tilemap.GetTile(new Vector3Int(0, 0, 0)), Is.Null);
            Assert.That(fixture.State.GetWaterDepth(Vector2Int.zero), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(fixture.State.DirtyCells, Is.Empty);
        });
    }
}
