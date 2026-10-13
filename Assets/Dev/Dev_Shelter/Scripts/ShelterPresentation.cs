using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Shelter-specific presentation for the staged Dev refactor. It renders
/// candidates, zone outlines, ghost visuals, labels, and placed shelter sprites.
/// </summary>
public class ShelterPresentation : MonoBehaviour
{
    [Header("Outline References")]
    [SerializeField] private ZoneThinOutlineByHover zoneOutlineHighlighter;

    [Header("Candidate Visualization")]
    [SerializeField] private Color candidateTileColor = new(0.15f, 0.9f, 1f, 1f);
    [SerializeField] private Color candidateAssociatedZoneColor = new(0f, 0.8f, 1f, 0.45f);
    [SerializeField] private Color placedShelterAssociatedZoneColor = new(0f, 1f, 0.65f, 0.75f);
    [SerializeField] private bool showPlacedShelterZoneOutlines = true;
    [SerializeField] private bool showCandidateLabels = true;
    [SerializeField] private Transform labelParent;
    [SerializeField] private TMP_FontAsset labelFontAsset;
    [SerializeField] private float labelHeightOffset = 0.85f;
    [SerializeField] private float labelFontSize = 3.25f;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private int labelSortingOrder = 2150;

    [Header("Shelter Visuals")]
    [SerializeField] private GameObject shelterGhostPrefab;
    [SerializeField] private SpriteRenderer shelterGhostRenderer;
    [SerializeField] private Sprite defaultShelterSprite;
    [SerializeField] private GameObject placedShelterPrefab;
    [SerializeField] private Color validPlacementColor = new(0.35f, 1f, 0.35f, 0.75f);
    [SerializeField] private Color invalidPlacementColor = new(1f, 0.2f, 0.2f, 0.55f);
    [SerializeField] private float ghostHeightOffset = 0.25f;
    [SerializeField] private int ghostSortingOrder = 2200;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<GameObject> _spawnedLabels = new();
    private readonly List<GameObject> _placedShelterObjects = new();

    private GameObject _runtimeGhostObject;
    private SpriteRenderer _runtimeGhostRenderer;
    private SpriteRenderer[] _runtimeGhostRenderers = Array.Empty<SpriteRenderer>();
    private Sprite _generatedFallbackShelterSprite;
    private bool _loggedMissingDefaultShelterSprite;

    public bool HasConfiguredShelterPlacementVisuals =>
        shelterGhostPrefab != null || shelterGhostRenderer != null || defaultShelterSprite != null;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        ClearCandidateVisuals();
        ClearPlacedShelterZoneOutlines();
        SetGhostVisible(false);
    }

    private void OnValidate()
    {
        labelFontSize = Mathf.Max(0.1f, labelFontSize);
        ghostHeightOffset = Mathf.Max(0f, ghostHeightOffset);
    }

    public void ResolveReferences()
    {
        if (zoneOutlineHighlighter == null)
            zoneOutlineHighlighter = FindFirstObjectByType<ZoneThinOutlineByHover>();

        if (labelParent == null)
            labelParent = transform;
    }

    public void ShowShelterCandidates(
        IReadOnlyList<ShelterCandidateResult> candidates,
        Func<Vector3Int, bool> canPlaceAtTile,
        Func<string, bool> isZoneEnabledForShelterPlacement)
    {
        ResolveReferences();
        ClearCandidateVisuals();

        if (candidates == null || candidates.Count == 0)
        {
            if (debugLogs)
                Debug.Log("[ShelterPresentation] No shelter candidates are cached to show.");

            return;
        }

        List<ZoneRiskOutlineRequest> zoneRequests = new();
        List<TileOutlineRequest> tileRequests = new();

        for (int i = 0; i < candidates.Count; i++)
        {
            ShelterCandidateResult result = candidates[i];

            if (result == null || !result.isValid)
                continue;

            if (canPlaceAtTile != null && !canPlaceAtTile(result.tileCell))
                continue;

            tileRequests.Add(new TileOutlineRequest(result.tileCell, candidateTileColor));

            if (result.associatedZoneGeoids != null)
            {
                for (int zoneIndex = 0; zoneIndex < result.associatedZoneGeoids.Count; zoneIndex++)
                {
                    string geoid = ShelterMetrics.NormalizeGeoid(result.associatedZoneGeoids[zoneIndex]);

                    if (string.IsNullOrEmpty(geoid) ||
                        (isZoneEnabledForShelterPlacement != null && !isZoneEnabledForShelterPlacement(geoid)))
                    {
                        continue;
                    }

                    zoneRequests.Add(new ZoneRiskOutlineRequest(geoid, candidateAssociatedZoneColor));
                }
            }

            if (showCandidateLabels)
                SpawnCandidateLabel(result);
        }

        if (zoneOutlineHighlighter != null)
        {
            zoneOutlineHighlighter.ShowShelterAssociatedZoneOutlines(zoneRequests);
            zoneOutlineHighlighter.ShowShelterCandidateTiles(tileRequests);
        }

        if (debugLogs)
            Debug.Log($"[ShelterPresentation] Showing {tileRequests.Count} shelter candidate tile highlights.");
    }

    public void ClearCandidateVisuals()
    {
        if (zoneOutlineHighlighter != null)
            zoneOutlineHighlighter.ClearShelterCandidateHighlights();

        for (int i = 0; i < _spawnedLabels.Count; i++)
        {
            if (_spawnedLabels[i] != null)
                Destroy(_spawnedLabels[i]);
        }

        _spawnedLabels.Clear();
    }

    public void RefreshPlacedShelterZoneOutlines(
        IReadOnlyList<PlacedShelterData> activeShelters,
        Func<string, bool> isZoneEnabledForShelterPlacement)
    {
        ResolveReferences();

        if (zoneOutlineHighlighter == null)
            return;

        if (!showPlacedShelterZoneOutlines)
        {
            zoneOutlineHighlighter.ClearPlacedShelterAssociatedZoneOutlines();
            return;
        }

        List<ZoneRiskOutlineRequest> zoneRequests = new();
        HashSet<string> processedGeoids = new(StringComparer.Ordinal);

        if (activeShelters != null)
        {
            for (int i = 0; i < activeShelters.Count; i++)
            {
                PlacedShelterData shelter = activeShelters[i];

                if (shelter == null || !shelter.isActive || shelter.associatedZoneGeoids == null)
                    continue;

                for (int zoneIndex = 0; zoneIndex < shelter.associatedZoneGeoids.Count; zoneIndex++)
                {
                    string geoid = ShelterMetrics.NormalizeGeoid(shelter.associatedZoneGeoids[zoneIndex]);

                    if (string.IsNullOrEmpty(geoid) ||
                        !processedGeoids.Add(geoid) ||
                        (isZoneEnabledForShelterPlacement != null && !isZoneEnabledForShelterPlacement(geoid)))
                    {
                        continue;
                    }

                    zoneRequests.Add(new ZoneRiskOutlineRequest(geoid, placedShelterAssociatedZoneColor));
                }
            }
        }

        zoneOutlineHighlighter.ShowPlacedShelterAssociatedZoneOutlines(zoneRequests);

        if (debugLogs)
            Debug.Log("[ShelterPresentation] Placed shelter zone outlines refreshed.");
    }

    public void ClearPlacedShelterZoneOutlines()
    {
        if (zoneOutlineHighlighter != null)
            zoneOutlineHighlighter.ClearPlacedShelterAssociatedZoneOutlines();
    }

    public void UpdateGhostVisual(
        Vector3Int cell,
        ShelterCandidateResult candidate,
        bool isValidPlacement,
        Tilemap placementTilemap,
        ShelterTypeDefinition selectedShelterType)
    {
        if (!EnsureGhostRenderer(selectedShelterType))
            return;

        if (placementTilemap == null)
        {
            Debug.LogWarning("[ShelterPresentation] Shelter ghost cannot follow cursor because no placement tilemap is assigned.");
            return;
        }

        Vector3 worldPosition = candidate != null && candidate.isValid
            ? candidate.worldPosition
            : placementTilemap.GetCellCenterWorld(cell);

        _runtimeGhostObject.transform.position = worldPosition + new Vector3(0f, ghostHeightOffset, 0f);
        PrepareGhostVisuals(isValidPlacement ? validPlacementColor : invalidPlacementColor, selectedShelterType);
        SetGhostVisible(true);
    }

    public bool PrepareGhost(ShelterTypeDefinition selectedShelterType)
    {
        bool hasGhost = EnsureGhostRenderer(selectedShelterType);
        PrepareGhostVisuals(validPlacementColor, selectedShelterType);
        SetGhostVisible(hasGhost);
        return hasGhost;
    }

    public void SetGhostVisible(bool isVisible)
    {
        if (_runtimeGhostObject != null)
            _runtimeGhostObject.SetActive(isVisible);
    }

    public void SpawnPlacedShelter(PlacedShelterData shelterData, ShelterTypeDefinition shelterType)
    {
        if (shelterData == null)
            return;

        GameObject shelterObject;

        if (placedShelterPrefab != null)
        {
            shelterObject = Instantiate(placedShelterPrefab, shelterData.worldPosition, Quaternion.identity, transform);
        }
        else
        {
            Debug.LogWarning("[ShelterPresentation] Missing placed shelter prefab. Creating a runtime SpriteRenderer fallback.");
            shelterObject = new GameObject($"PlacedShelter_{shelterData.shelterTypeName}_{shelterData.tileCell.x}_{shelterData.tileCell.y}");
            shelterObject.transform.SetParent(transform, false);
            shelterObject.transform.position = shelterData.worldPosition;
            shelterObject.AddComponent<SpriteRenderer>();
        }

        PreparePlacedShelterVisuals(shelterObject, GetShelterSprite(shelterType));
        _placedShelterObjects.Add(shelterObject);
    }

    public Sprite GetShelterSprite(ShelterTypeDefinition shelterType)
    {
        if (shelterType != null && shelterType.shelterSprite != null)
            return shelterType.shelterSprite;

        if (defaultShelterSprite != null)
            return defaultShelterSprite;

        return GetGeneratedFallbackShelterSprite();
    }

    private bool EnsureGhostRenderer(ShelterTypeDefinition selectedShelterType)
    {
        if (_runtimeGhostObject != null && _runtimeGhostRenderers != null && _runtimeGhostRenderers.Length > 0)
            return true;

        if (shelterGhostPrefab != null)
        {
            _runtimeGhostObject = Instantiate(shelterGhostPrefab, transform);
            _runtimeGhostObject.name = "ShelterPlacementGhost";

            if (debugLogs)
                Debug.Log("[ShelterPresentation] Shelter ghost prefab instantiated.");
        }
        else if (shelterGhostRenderer != null)
        {
            _runtimeGhostRenderer = shelterGhostRenderer;
            _runtimeGhostObject = shelterGhostRenderer.gameObject;

            if (debugLogs)
                Debug.Log("[ShelterPresentation] Using assigned scene SpriteRenderer for shelter ghost.");
        }
        else
        {
            Debug.LogWarning("[ShelterPresentation] Missing shelter ghost prefab. Creating a runtime SpriteRenderer fallback.");
            _runtimeGhostObject = new GameObject("ShelterPlacementGhost");
            _runtimeGhostObject.transform.SetParent(transform, false);
            _runtimeGhostRenderer = _runtimeGhostObject.AddComponent<SpriteRenderer>();
        }

        if (_runtimeGhostObject == null)
            return false;

        _runtimeGhostRenderers = _runtimeGhostObject.GetComponentsInChildren<SpriteRenderer>(true);

        if ((_runtimeGhostRenderers == null || _runtimeGhostRenderers.Length == 0) && _runtimeGhostObject != null)
        {
            _runtimeGhostRenderer = _runtimeGhostObject.AddComponent<SpriteRenderer>();
            _runtimeGhostRenderers = new[] { _runtimeGhostRenderer };
            Debug.LogWarning("[ShelterPresentation] Shelter ghost prefab had no SpriteRenderer. Added a fallback SpriteRenderer.");
        }

        _runtimeGhostRenderer = _runtimeGhostRenderers != null && _runtimeGhostRenderers.Length > 0
            ? _runtimeGhostRenderers[0]
            : null;

        if (_runtimeGhostRenderer == null)
            return false;

        PrepareGhostVisuals(validPlacementColor, selectedShelterType);
        SetGhostVisible(false);
        return true;
    }

    private void PrepareGhostVisuals(Color color, ShelterTypeDefinition selectedShelterType)
    {
        if (_runtimeGhostObject == null)
            return;

        if (_runtimeGhostObject.transform.localScale.sqrMagnitude <= 0.0001f)
            _runtimeGhostObject.transform.localScale = Vector3.one;

        if (!_loggedMissingDefaultShelterSprite &&
            defaultShelterSprite == null &&
            (selectedShelterType == null || selectedShelterType.shelterSprite == null))
        {
            Debug.LogWarning("[ShelterPresentation] No default shelter sprite assigned. A generated fallback sprite will be used.");
            _loggedMissingDefaultShelterSprite = true;
        }

        ApplyGhostSprite(GetShelterSprite(selectedShelterType));

        for (int i = 0; i < _runtimeGhostRenderers.Length; i++)
        {
            SpriteRenderer renderer = _runtimeGhostRenderers[i];

            if (renderer == null)
                continue;

            renderer.enabled = true;
            renderer.sortingOrder = ghostSortingOrder;
            renderer.color = new Color(color.r, color.g, color.b, Mathf.Max(0.1f, color.a));
        }
    }

    private void ApplyGhostSprite(Sprite sprite)
    {
        if (_runtimeGhostRenderers == null || _runtimeGhostRenderers.Length == 0)
            return;

        Sprite safeSprite = sprite != null ? sprite : GetGeneratedFallbackShelterSprite();

        for (int i = 0; i < _runtimeGhostRenderers.Length; i++)
        {
            SpriteRenderer renderer = _runtimeGhostRenderers[i];

            if (renderer == null)
                continue;

            if (renderer.sprite == null)
                renderer.sprite = safeSprite;
        }
    }

    private void PreparePlacedShelterVisuals(GameObject shelterObject, Sprite shelterSprite)
    {
        if (shelterObject == null)
            return;

        shelterObject.SetActive(true);
        SpriteRenderer[] renderers = shelterObject.GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            renderers = new[] { shelterObject.AddComponent<SpriteRenderer>() };
            Debug.LogWarning("[ShelterPresentation] Placed shelter prefab had no SpriteRenderer. Added a fallback SpriteRenderer.");
        }

        Sprite safeSprite = shelterSprite != null ? shelterSprite : GetGeneratedFallbackShelterSprite();

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (renderer.sprite == null)
                renderer.sprite = safeSprite;

            renderer.enabled = true;
            renderer.sortingOrder = ghostSortingOrder - 1;
            renderer.color = Color.white;
        }
    }

    private Sprite GetGeneratedFallbackShelterSprite()
    {
        if (_generatedFallbackShelterSprite != null)
            return _generatedFallbackShelterSprite;

        const int size = 16;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = "GeneratedShelterFallbackSprite";
        _generatedFallbackShelterSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _generatedFallbackShelterSprite;
    }

    private void SpawnCandidateLabel(ShelterCandidateResult result)
    {
        Transform parent = labelParent != null ? labelParent : transform;
        GameObject labelObject = new($"ShelterCandidateLabel_{result.candidateId}");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = result.worldPosition + new Vector3(0f, labelHeightOffset, 0f);

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            if (labelObject.transform.position.z <= mainCamera.transform.position.z)
            {
                Vector3 position = labelObject.transform.position;
                position.z = mainCamera.transform.position.z + 1f;
                labelObject.transform.position = position;
            }

            labelObject.transform.rotation = mainCamera.transform.rotation;
        }

        TextMeshPro textLabel = labelObject.AddComponent<TextMeshPro>();

        if (labelFontAsset != null)
            textLabel.font = labelFontAsset;

        textLabel.alignment = TextAlignmentOptions.Center;
        textLabel.fontSize = labelFontSize;
        textLabel.enableWordWrapping = false;
        textLabel.overflowMode = TextOverflowModes.Overflow;
        textLabel.color = labelColor;
        textLabel.text = $"Shelter Candidate\nServes {result.zoneCount} zones";

        Renderer textRenderer = textLabel.GetComponent<Renderer>();

        if (textRenderer != null)
            textRenderer.sortingOrder = labelSortingOrder;

        _spawnedLabels.Add(labelObject);
    }
}
