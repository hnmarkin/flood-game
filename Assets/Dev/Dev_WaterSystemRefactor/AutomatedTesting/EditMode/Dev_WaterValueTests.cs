using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

[Category("WaterUnit")]
public sealed class Dev_WaterValueTests : Dev_WaterEditModeFixture
{
    // Tests: renderer resolution

    [Test]
    public void RendererDefinition_BandThreshold_ResolvesWithoutGap()
    {
        // Arrange
        Tile shallow = Track(ScriptableObject.CreateInstance<Tile>());
        Tile deep = Track(ScriptableObject.CreateInstance<Tile>());
        Dev_RendererDef renderer = CreateRenderer(
            bands: new[]
            {
                new Dev_WaterVisualBand { minimumDepth = 0.001f, maximumDepth = 2f, tile = shallow, tint = Color.green },
                new Dev_WaterVisualBand { minimumDepth = 2f, maximumDepth = 1000f, tile = deep, tint = Color.blue }
            });

        // Act
        TileBase resolved = renderer.ResolveTile(2.0005f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(resolved, Is.SameAs(deep));
            Assert.That(renderer.ResolveTint(2.0005f), Is.EqualTo(Color.blue));
        });
    }

    [Test]
    public void RendererDefinition_NullBandAndUnmatchedDepth_FallsBackToDryValues()
    {
        // Arrange
        Tile dry = Track(ScriptableObject.CreateInstance<Tile>());
        Color dryTint = new Color(0.2f, 0.3f, 0.4f, 1f);
        Dev_RendererDef renderer = CreateRenderer(
            dry,
            dryTint,
            new Dev_WaterVisualBand[] { null, new Dev_WaterVisualBand { minimumDepth = 2f, maximumDepth = 3f } });

        // Act
        TileBase resolved = renderer.ResolveTile(1f);

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(resolved, Is.SameAs(dry));
            Assert.That(renderer.ResolveTint(1f), Is.EqualTo(dryTint));
        });
    }

    // Tests: immutable projection value

    [Test]
    public void WaterProjection_ConstructorInputMutation_DoesNotChangeStoredDepths()
    {
        // Arrange
        float[] depths = { 1f, 2f, 3f, 4f };
        Dev_WaterProjection projection = new Dev_WaterProjection(
            new Vector2Int(3, -1),
            2,
            2,
            Dev_WaterProfileStage.Crisis,
            5f,
            depths);

        // Act
        depths[0] = 99f;

        // Assert
        Dev_WaterAssert.Multiple(() =>
        {
            Assert.That(projection.GetWaterDepth(new Vector2Int(3, -1)), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(projection.GetWaterDepth(new Vector2Int(4, 0)), Is.EqualTo(4f).Within(Tolerance));
            Assert.That(projection.GetWaterDepth(new Vector2Int(2, -1)), Is.Zero.Within(Tolerance));
            Assert.That(projection.ProfileStage, Is.EqualTo(Dev_WaterProfileStage.Crisis));
            Assert.That(projection.SimulatedDuration, Is.EqualTo(5f).Within(Tolerance));
        });
    }
}
