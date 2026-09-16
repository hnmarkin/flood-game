using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryToolsController : MonoBehaviour
{
    private const string ToolSlotClassName = "inventory-tool-slot";
    private const string ShelterToolName = "inventory_tool_2";
    private const string InventorySelectedClassName = "inventory-tool-selected";
    private const string ShelterSelectedClassName = "shelter-tool-selected";

    [Header("References")]
    [SerializeField] private UIDocument inventoryUIDocument;
    [SerializeField] private ShelterCandidateController shelterCandidateController;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<VisualElement> _toolSlots = new();
    private VisualElement _shelterTool;
    private Coroutine _bindRoutine;
    private bool _isBound;
    private bool _isSubscribedToPlacementMode;
    private int _lastActivationFrame = -1;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        TryBindInventoryTools();

        if (!_isBound && _bindRoutine == null)
            _bindRoutine = StartCoroutine(BindInventoryToolsWhenReady());

        SubscribeToPlacementMode();
    }

    private void OnDisable()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        UnbindInventoryTools();
        UnsubscribeFromPlacementMode();
    }

    private void ResolveReferences()
    {
        if (inventoryUIDocument == null)
        {
            inventoryUIDocument = GetComponent<UIDocument>();

            if (debugLogs && inventoryUIDocument != null)
                Debug.Log("[InventoryToolsController] Inventory UIDocument found on this GameObject.");
        }

        if (shelterCandidateController == null)
            shelterCandidateController = FindBestShelterCandidateController();
    }

    private IEnumerator BindInventoryToolsWhenReady()
    {
        const int maxFramesToWait = 30;
        int waitedFrames = 0;

        while (!_isBound && waitedFrames < maxFramesToWait)
        {
            TryBindInventoryTools();

            if (_isBound)
                break;

            waitedFrames++;
            yield return null;
        }

        if (!_isBound)
            Debug.LogWarning("[InventoryToolsController] Could not bind inventory_tool_2 from the assigned UIDocument.");

        _bindRoutine = null;
    }

    private void TryBindInventoryTools()
    {
        if (_isBound || inventoryUIDocument == null)
            return;

        VisualElement root = inventoryUIDocument.rootVisualElement;

        if (root == null)
            return;

        if (debugLogs)
            Debug.Log("[InventoryToolsController] Inventory UIDocument root loaded.");

        _toolSlots.Clear();

        for (int i = 1; i <= 5; i++)
        {
            VisualElement toolSlot = root.Q<VisualElement>($"inventory_tool_{i}");

            if (toolSlot == null)
                continue;

            toolSlot.AddToClassList(ToolSlotClassName);
            _toolSlots.Add(toolSlot);
        }

        _shelterTool = root.Q<VisualElement>(ShelterToolName);

        if (_shelterTool == null)
        {
            Debug.LogWarning("[InventoryToolsController] inventory_tool_2 was not found in the inventory UIDocument.");
            return;
        }

        _shelterTool.pickingMode = PickingMode.Position;
        SetChildPickingMode(_shelterTool, PickingMode.Ignore);
        _shelterTool.RegisterCallback<ClickEvent>(OnShelterToolClicked, TrickleDown.TrickleDown);
        _shelterTool.RegisterCallback<PointerDownEvent>(OnShelterToolPointerDown, TrickleDown.TrickleDown);
        _isBound = true;
        SetShelterSelected(shelterCandidateController != null && shelterCandidateController.IsShelterPlacementModeActive);

        if (debugLogs)
        {
            Debug.Log("[InventoryToolsController] inventory_tool_2 found.");
            Debug.Log("[InventoryToolsController] Click and pointer-down callbacks registered for inventory_tool_2.");
        }
    }

    private void UnbindInventoryTools()
    {
        if (_isBound && _shelterTool != null)
        {
            _shelterTool.UnregisterCallback<ClickEvent>(OnShelterToolClicked, TrickleDown.TrickleDown);
            _shelterTool.UnregisterCallback<PointerDownEvent>(OnShelterToolPointerDown, TrickleDown.TrickleDown);
        }

        _shelterTool = null;
        _toolSlots.Clear();
        _isBound = false;
    }

    private void SubscribeToPlacementMode()
    {
        if (_isSubscribedToPlacementMode || shelterCandidateController == null)
            return;

        shelterCandidateController.ShelterPlacementModeChanged += OnShelterPlacementModeChanged;
        _isSubscribedToPlacementMode = true;
    }

    private void UnsubscribeFromPlacementMode()
    {
        if (!_isSubscribedToPlacementMode || shelterCandidateController == null)
            return;

        shelterCandidateController.ShelterPlacementModeChanged -= OnShelterPlacementModeChanged;
        _isSubscribedToPlacementMode = false;
    }

    private void OnShelterToolClicked(ClickEvent clickEvent)
    {
        clickEvent.StopPropagation();
        ActivateShelterPlacementFromInventory("ClickEvent");
    }

    private void OnShelterToolPointerDown(PointerDownEvent pointerDownEvent)
    {
        if (pointerDownEvent.button != 0)
            return;

        pointerDownEvent.StopPropagation();
        ActivateShelterPlacementFromInventory("PointerDownEvent");
    }

    private void ActivateShelterPlacementFromInventory(string eventSource)
    {
        if (_lastActivationFrame == Time.frameCount)
            return;

        _lastActivationFrame = Time.frameCount;

        if (debugLogs)
            Debug.Log($"[InventoryToolsController] inventory_tool_2 clicked via {eventSource}.");

        if (shelterCandidateController == null)
        {
            shelterCandidateController = FindBestShelterCandidateController();
            SubscribeToPlacementMode();
        }

        if (shelterCandidateController == null)
        {
            Debug.LogWarning("[InventoryToolsController] ShelterCandidateController is not assigned.");
            return;
        }

        shelterCandidateController.ToggleShelterPlacementMode();
        SetShelterSelected(shelterCandidateController.IsShelterPlacementModeActive);

        if (debugLogs)
            Debug.Log($"[InventoryToolsController] Shelter placement mode toggle requested. Active={shelterCandidateController.IsShelterPlacementModeActive}.");
    }

    private void OnShelterPlacementModeChanged(bool isActive)
    {
        SetShelterSelected(isActive);
    }

    private void SetShelterSelected(bool isSelected)
    {
        if (_shelterTool == null)
            return;

        if (isSelected)
        {
            _shelterTool.AddToClassList(InventorySelectedClassName);
            _shelterTool.AddToClassList(ShelterSelectedClassName);
        }
        else
        {
            _shelterTool.RemoveFromClassList(InventorySelectedClassName);
            _shelterTool.RemoveFromClassList(ShelterSelectedClassName);
        }
    }

    private ShelterCandidateController FindBestShelterCandidateController()
    {
        ShelterCandidateController[] controllers = FindObjectsByType<ShelterCandidateController>(FindObjectsSortMode.None);

        if (controllers == null || controllers.Length == 0)
            return null;

        ShelterCandidateController firstController = controllers[0];

        for (int i = 0; i < controllers.Length; i++)
        {
            ShelterCandidateController controller = controllers[i];

            if (controller != null && controller.HasConfiguredShelterPlacementVisuals)
                return controller;
        }

        return firstController;
    }

    private void SetChildPickingMode(VisualElement parent, PickingMode pickingMode)
    {
        if (parent == null)
            return;

        foreach (VisualElement child in parent.Children())
        {
            child.pickingMode = pickingMode;
            SetChildPickingMode(child, pickingMode);
        }
    }
}
