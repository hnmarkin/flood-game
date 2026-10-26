using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>
/// Temporary Shelter input bridge for the staged Dev refactor. It follows the
/// project's current New Input System pattern and delegates all rules to the controller.
/// </summary>
public class ShelterPlacementInput : MonoBehaviour
{
    [SerializeField] private ShelterController shelterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Tilemap placementTilemap;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (shelterController == null || !shelterController.IsShelterPlacementModeActive)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            shelterController.ExitShelterPlacementMode();
            return;
        }

        if (!TryGetPointerTileCell(out Vector3Int hoveredCell))
        {
            shelterController.HandlePlacementPointerUnavailable();
            return;
        }

        shelterController.HandlePlacementHover(hoveredCell);

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            shelterController.HandlePlacementClick(hoveredCell);
    }

    public void ResolveReferences()
    {
        if (shelterController == null)
            shelterController = FindFirstObjectByType<ShelterController>();

        if (mainCamera == null)
            mainCamera = shelterController != null ? shelterController.MainCamera : Camera.main;

        if (placementTilemap == null && shelterController != null)
            placementTilemap = shelterController.PlacementTilemap;
    }

    private bool TryGetPointerTileCell(out Vector3Int cell)
    {
        cell = default;

        if (Mouse.current == null)
            return false;

        if (mainCamera == null && shelterController != null)
            mainCamera = shelterController.MainCamera;

        if (placementTilemap == null && shelterController != null)
            placementTilemap = shelterController.PlacementTilemap;

        if (mainCamera == null || placementTilemap == null)
            return false;

        Vector3 screenPosition = Mouse.current.position.ReadValue();
        screenPosition.z = -mainCamera.transform.position.z;
        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        world.z = 0f;
        cell = placementTilemap.WorldToCell(world);
        return true;
    }
}
