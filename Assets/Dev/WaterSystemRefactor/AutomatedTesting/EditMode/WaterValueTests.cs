using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

[Category("WaterUnit")]
public sealed class WaterValueTests : WaterEditModeFixture
{
    // Tests: renderer resolution

    [Test]
    public void RendererDefinition_BandThreshold_ResolvesWithoutGap()
    {
        // Arrange
        Tile shallow = Track(ScriptableObject.CreateInstance<Tile>());
        Tile deep = Track(ScriptableObject.CreateInstance<Tile>());
        RendererDef renderer = CreateRenderer(
            bands: new[]
            {
                new WaterVisualBand { minimumDepth = 0.001f, maximumDepth = 2f, tile = shallow, tint = Color.green },
                new WaterVisualBand { minimumDepth = 2f, maximumDepth = 1000f, tile = deep, tint = Color.blue }
            });

        // Act
        TileBase resolved = renderer.ResolveTile(2.0005f);

        // Assert
        WaterAssert.Multiple(() =>
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
        RendererDef renderer = CreateRenderer(
            dry,
            dryTint,
            new WaterVisualBand[] { null, new WaterVisualBand { minimumDepth = 2f, maximumDepth = 3f } });

        // Act
        TileBase resolved = renderer.ResolveTile(1f);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(resolved, Is.SameAs(dry));
            Assert.That(renderer.ResolveTint(1f), Is.EqualTo(dryTint));
        });
    }

    [Test]
    public void RendererDefinition_DepthBand_ResolvesOneCompleteFloodedTileVariant()
    {
        // Arrange
        Tile dry = Track(ScriptableObject.CreateInstance<Tile>());
        Tile shallowFloodedBuilding = Track(ScriptableObject.CreateInstance<Tile>());
        Tile deepFloodedBuilding = Track(ScriptableObject.CreateInstance<Tile>());
        RendererDef renderer = CreateRenderer(
            dry,
            Color.white,
            new[]
            {
                new WaterVisualBand
                {
                    minimumDepth = 0.001f,
                    maximumDepth = 1f,
                    tile = shallowFloodedBuilding,
                    tint = Color.cyan
                },
                new WaterVisualBand
                {
                    minimumDepth = 1f,
                    maximumDepth = 2f,
                    tile = deepFloodedBuilding,
                    tint = Color.blue
                }
            });

        // Act
        WaterVisual dryVisual = renderer.ResolveVisual(0f);
        WaterVisual floodedVisual = renderer.ResolveVisual(1.5f);
        WaterVisual saturatedVisual = renderer.ResolveVisual(10f);

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(dryVisual.Tile, Is.SameAs(dry));
            Assert.That(floodedVisual.Tile, Is.SameAs(deepFloodedBuilding));
            Assert.That(floodedVisual.Tint, Is.EqualTo(Color.blue));
            Assert.That(saturatedVisual.Tile, Is.SameAs(deepFloodedBuilding));
            Assert.That(saturatedVisual.Tint, Is.EqualTo(Color.blue));
        });
    }

    [Test]
    public void RendererDefinition_ProductionValidation_RejectsMissingReplacementTileOrBandGap()
    {
        // Arrange
        Tile dry = Track(ScriptableObject.CreateInstance<Tile>());
        RendererDef missingReplacement = CreateRenderer(
            dry,
            Color.white,
            new[] { new WaterVisualBand { minimumDepth = 0.001f, maximumDepth = 1f } });
        RendererDef gap = CreateRenderer(
            dry,
            Color.white,
            new[]
            {
                new WaterVisualBand
                {
                    minimumDepth = 0.001f,
                    maximumDepth = 1f,
                    tile = Track(ScriptableObject.CreateInstance<Tile>())
                },
                new WaterVisualBand
                {
                    minimumDepth = 2f,
                    maximumDepth = 3f,
                    tile = Track(ScriptableObject.CreateInstance<Tile>())
                }
            });

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(missingReplacement.IsValidForProduction(out _), Is.False);
            Assert.That(gap.IsValidForProduction(out _), Is.False);
        });
    }

    // Tests: immutable projection value

    [Test]
    public void WaterProjection_ConstructorInputMutation_DoesNotChangeStoredDepths()
    {
        // Arrange
        float[] depths = { 1f, 2f, 3f, 4f };
        WaterProjection projection = new WaterProjection(
            new Vector2Int(3, -1),
            2,
            2,
            WaterProfileStage.Crisis,
            5f,
            depths);

        // Act
        depths[0] = 99f;

        // Assert
        WaterAssert.Multiple(() =>
        {
            Assert.That(projection.GetWaterDepth(new Vector2Int(3, -1)), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(projection.GetWaterDepth(new Vector2Int(4, 0)), Is.EqualTo(4f).Within(Tolerance));
            Assert.That(projection.GetWaterDepth(new Vector2Int(2, -1)), Is.Zero.Within(Tolerance));
            Assert.That(projection.ProfileStage, Is.EqualTo(WaterProfileStage.Crisis));
            Assert.That(projection.SimulatedDuration, Is.EqualTo(5f).Within(Tolerance));
        });
    }
}
