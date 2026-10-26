using System.Collections.Generic;
using UnityEngine;

/// <summary>Tower render objects only; activation and statuses remain in CommunicationState.</summary>
internal sealed class CommunicationTowerVisuals
{
    internal readonly struct Settings
    {
        internal readonly Sprite mainTowerSprite;
        internal readonly Sprite zoneTowerSprite;
        internal readonly Color inactiveTowerColor;
        internal readonly Color activeTowerColor;
        internal readonly Color hoveredTowerColor;
        internal readonly float towerVisualScale;
        internal readonly float towerHeightOffset;
        internal readonly string towerSortingLayerName;
        internal readonly int towerSortingOrder;

        internal Settings(
            Sprite mainTowerSprite, Sprite zoneTowerSprite,
            Color inactiveTowerColor, Color activeTowerColor, Color hoveredTowerColor,
            float towerVisualScale, float towerHeightOffset,
            string towerSortingLayerName, int towerSortingOrder)
        {
            this.mainTowerSprite = mainTowerSprite;
            this.zoneTowerSprite = zoneTowerSprite;
            this.inactiveTowerColor = inactiveTowerColor;
            this.activeTowerColor = activeTowerColor;
            this.hoveredTowerColor = hoveredTowerColor;
            this.towerVisualScale = towerVisualScale;
            this.towerHeightOffset = towerHeightOffset;
            this.towerSortingLayerName = towerSortingLayerName;
            this.towerSortingOrder = towerSortingOrder;
        }
    }

    private readonly Dictionary<string, GameObject> _towerVisualById = new();
    private readonly Dictionary<string, SpriteRenderer> _towerRendererById = new();
    private bool _loggedMissingSpriteWarning;

    internal void SpawnTowerVisual(CommunicationTowerData tower, Transform towerVisualRoot, Settings settings)
    {
        if (tower == null || string.IsNullOrEmpty(tower.towerId))
            return;

        GameObject towerObject = new($"CommunicationTower_{tower.towerId}");
        towerObject.transform.SetParent(towerVisualRoot, false);
        Vector3 visualPosition = tower.worldPosition;
        visualPosition.y += settings.towerHeightOffset;
        towerObject.transform.position = visualPosition;
        towerObject.transform.localScale = Vector3.one * settings.towerVisualScale;

        SpriteRenderer spriteRenderer = towerObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForTower(tower, settings);
        spriteRenderer.color = tower.isActive ? settings.activeTowerColor : settings.inactiveTowerColor;
        spriteRenderer.sortingLayerName = settings.towerSortingLayerName;
        spriteRenderer.sortingOrder = settings.towerSortingOrder;

        if (spriteRenderer.sprite == null && !_loggedMissingSpriteWarning)
        {
            _loggedMissingSpriteWarning = true;
            Debug.LogWarning("[CommunicationTowerController] Main Tower Sprite and Zone Tower Sprite are not assigned. Empty tower placeholder objects will still be spawned.");
        }

        _towerVisualById[tower.towerId] = towerObject;
        _towerRendererById[tower.towerId] = spriteRenderer;
    }

    internal void UpdateTowerVisual(CommunicationTowerData tower, CommunicationTowerData hoveredTower, Settings settings)
    {
        if (tower == null || string.IsNullOrEmpty(tower.towerId))
            return;

        if (!_towerRendererById.TryGetValue(tower.towerId, out SpriteRenderer spriteRenderer) || spriteRenderer == null)
            return;

        spriteRenderer.sprite = GetSpriteForTower(tower, settings);
        spriteRenderer.color = tower == hoveredTower
            ? settings.hoveredTowerColor
            : tower.isActive
                ? settings.activeTowerColor
                : settings.inactiveTowerColor;
    }

    private Sprite GetSpriteForTower(CommunicationTowerData tower, Settings settings)
    {
        if (tower != null && tower.isActive)
            return settings.zoneTowerSprite != null ? settings.zoneTowerSprite : settings.mainTowerSprite;

        return settings.mainTowerSprite != null ? settings.mainTowerSprite : settings.zoneTowerSprite;
    }

    internal void ClearTowerVisuals()
    {
        foreach (KeyValuePair<string, GameObject> pair in _towerVisualById)
        {
            if (pair.Value != null)
                UnityEngine.Object.Destroy(pair.Value);
        }

        _towerVisualById.Clear();
        _towerRendererById.Clear();
    }

    internal void SetTowerVisualsVisible(
        IReadOnlyList<CommunicationTowerData> towers, bool visible, bool keepActiveTowersVisible = false)
    {
        for (int i = 0; i < towers.Count; i++)
        {
            CommunicationTowerData tower = towers[i];

            if (tower == null || string.IsNullOrEmpty(tower.towerId))
                continue;

            if (!_towerVisualById.TryGetValue(tower.towerId, out GameObject visual) || visual == null)
                continue;

            visual.SetActive(visible || (keepActiveTowersVisible && tower.isActive));
        }
    }
}
