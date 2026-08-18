using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ActionsPanelController : MonoBehaviour
{
    private const string PlaceBarrierButtonName = "action_Button1";
    private const string ShelterCandidateButtonName = "action_Button2";
    private const string EvacuationButtonName = "action_Button5";
    private const string CommunicationButtonName = "action_Button6";
    private const string PreparationCardsButtonName = "action_Button7";
    private const string ActiveButtonClassName = "action-button-active";

    [Header("References")]
    [SerializeField] private UIDocument actionsUIDocument;
    [SerializeField] private FloodDefenseBoxStamp floodDefenseBoxStamp;
    [SerializeField] private ShelterCandidateController shelterCandidateController;
    [SerializeField] private EvacuationController evacuationController;
    [SerializeField] private CommunicationTowerController communicationTowerController;
    [SerializeField] private PreparationCardsController preparationCardsController;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Button _placeBarrierButton;
    private Button _shelterCandidateButton;
    private Button _evacuationButton;
    private Button _communicationButton;
    private Button _preparationCardsButton;
    private Coroutine _bindRoutine;
    private bool _placeBarrierBound;
    private bool _shelterCandidateBound;
    private bool _evacuationBound;
    private bool _communicationBound;
    private bool _preparationCardsBound;
    private bool _isSubscribedToBarrierMode;
    private bool _isSubscribedToCommunicationMode;
    private bool _isSubscribedToCardsUI;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        TryBindButtons();

        if (!AreButtonsBound() && _bindRoutine == null)
            _bindRoutine = StartCoroutine(BindButtonsWhenReady());

        SubscribeToExternalState();
        RefreshButtonStates();
    }

    private void OnDisable()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        UnsubscribeFromExternalState();
        UnbindButtons();
    }

    public void EnterPlaceBarriersMode()
    {
        if (floodDefenseBoxStamp == null)
        {
            Debug.LogWarning("[ActionsPanelController] FloodDefenseBoxStamp is not assigned.");
            return;
        }

        floodDefenseBoxStamp.EnablePlaceBarrierMode();

        if (debugLogs)
            Debug.Log("[ActionsPanelController] Requested Place Barriers mode from FloodDefenseBoxStamp.");
    }

    public void TogglePlaceBarriersMode()
    {
        if (floodDefenseBoxStamp == null)
        {
            floodDefenseBoxStamp = FindFirstObjectByType<FloodDefenseBoxStamp>();
            SubscribeToExternalState();
        }

        if (floodDefenseBoxStamp == null)
        {
            Debug.LogWarning("[ActionsPanelController] FloodDefenseBoxStamp is not assigned.");
            return;
        }

        floodDefenseBoxStamp.ToggleBuildModeFromUI();
        RefreshButtonStates();

        if (debugLogs)
            Debug.Log("[ActionsPanelController] action_Button1 toggled Place Barriers mode.");
    }

    public void ToggleShelterCandidateMode()
    {
        if (shelterCandidateController == null)
        {
            Debug.LogWarning("[ActionsPanelController] ShelterCandidateController is not assigned.");
            return;
        }

        shelterCandidateController.ToggleShelterCandidates();

        if (debugLogs)
            Debug.Log("[ActionsPanelController] Toggled Shelter Candidate mode.");
    }

    public void ToggleEvacuationPreview()
    {
        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (evacuationController == null)
        {
            Debug.LogWarning("[ActionsPanelController] EvacuationController is not assigned.");
            return;
        }

        evacuationController.ToggleEvacuationPreview();
        RefreshButtonStates();

        if (debugLogs)
            Debug.Log("[ActionsPanelController] Toggled Evacuation Preview mode.");
    }

    public void ToggleEvacuationPreviewMode()
    {
        ToggleEvacuationPreview();
    }

    public void ToggleCommunicationMode()
    {
        if (communicationTowerController == null)
        {
            communicationTowerController = FindFirstObjectByType<CommunicationTowerController>();

            if (communicationTowerController == null)
                communicationTowerController = CreateRuntimeCommunicationTowerController();

            SubscribeToExternalState();
        }

        if (communicationTowerController == null)
        {
            Debug.LogWarning("[ActionsPanelController] CommunicationTowerController is not assigned.");
            return;
        }

        communicationTowerController.ToggleCommunicationMode();
        RefreshButtonStates();

        if (debugLogs)
            Debug.Log("[ActionsPanelController] action_Button6 toggled Communication mode.");
    }

    public void TogglePreparationCardsUI()
    {
        if (preparationCardsController == null)
        {
            preparationCardsController = FindFirstObjectByType<PreparationCardsController>();
            SubscribeToExternalState();
        }

        if (preparationCardsController == null)
        {
            Debug.LogWarning("[ActionsPanelController] PreparationCardsController is not assigned.");
            return;
        }

        preparationCardsController.ToggleCardsUI();
        RefreshButtonStates();

        if (debugLogs)
            Debug.Log("[ActionsPanelController] action_Button7 toggled PreparationCards UI.");
    }

    private void ResolveReferences()
    {
        if (actionsUIDocument == null)
            actionsUIDocument = GetComponent<UIDocument>();

        if (floodDefenseBoxStamp == null)
            floodDefenseBoxStamp = FindFirstObjectByType<FloodDefenseBoxStamp>();

        if (shelterCandidateController == null)
            shelterCandidateController = FindFirstObjectByType<ShelterCandidateController>();

        if (evacuationController == null)
            evacuationController = FindFirstObjectByType<EvacuationController>();

        if (communicationTowerController == null)
            communicationTowerController = FindFirstObjectByType<CommunicationTowerController>();

        if (communicationTowerController == null)
            communicationTowerController = CreateRuntimeCommunicationTowerController();

        if (preparationCardsController == null)
            preparationCardsController = FindFirstObjectByType<PreparationCardsController>();
    }

    private CommunicationTowerController CreateRuntimeCommunicationTowerController()
    {
        GameObject controllerObject = new GameObject("CommunicationTowerController");
        CommunicationTowerController controller = controllerObject.AddComponent<CommunicationTowerController>();

        if (debugLogs)
            Debug.Log("[ActionsPanelController] Created runtime CommunicationTowerController because none was present in the scene.");

        return controller;
    }

    private IEnumerator BindButtonsWhenReady()
    {
        const int maxFramesToWait = 30;
        int waitedFrames = 0;

        while (!AreButtonsBound() && waitedFrames < maxFramesToWait)
        {
            TryBindButtons();

            if (AreButtonsBound())
                break;

            waitedFrames++;
            yield return null;
        }

        if (!_placeBarrierBound)
            Debug.LogWarning("[ActionsPanelController] Could not bind action_Button1 from the assigned UIDocument.");

        if (!_shelterCandidateBound)
            Debug.LogWarning("[ActionsPanelController] Could not bind action_Button2 from the assigned UIDocument.");

        if (!_evacuationBound)
            Debug.LogWarning("[ActionsPanelController] Could not bind action_Button5 from the assigned UIDocument.");

        if (!_communicationBound)
            Debug.LogWarning("[ActionsPanelController] Could not bind action_Button6 from the assigned UIDocument.");

        if (!_preparationCardsBound)
            Debug.LogWarning("[ActionsPanelController] Could not bind action_Button7 from the assigned UIDocument.");

        _bindRoutine = null;
    }

    private void TryBindButtons()
    {
        if (actionsUIDocument == null)
            return;

        VisualElement root = actionsUIDocument.rootVisualElement;

        if (root == null)
            return;

        if (!_placeBarrierBound)
        {
            _placeBarrierButton = root.Q<Button>(PlaceBarrierButtonName);

            if (_placeBarrierButton != null)
            {
                _placeBarrierButton.clicked += OnPlaceBarrierClicked;
                _placeBarrierBound = true;
            }
        }

        if (!_shelterCandidateBound)
        {
            _shelterCandidateButton = root.Q<Button>(ShelterCandidateButtonName);

            if (_shelterCandidateButton != null)
            {
                _shelterCandidateButton.clicked += OnShelterCandidateClicked;
                _shelterCandidateBound = true;
            }
        }

        if (!_evacuationBound)
        {
            _evacuationButton = root.Q<Button>(EvacuationButtonName);

            if (_evacuationButton != null)
            {
                _evacuationButton.clicked += OnEvacuationClicked;
                _evacuationBound = true;
            }
        }

        if (!_preparationCardsBound)
        {
            _preparationCardsButton = root.Q<Button>(PreparationCardsButtonName);

            if (_preparationCardsButton != null)
            {
                _preparationCardsButton.clicked += OnPreparationCardsClicked;
                _preparationCardsBound = true;
            }
        }

        if (!_communicationBound)
        {
            _communicationButton = root.Q<Button>(CommunicationButtonName);

            if (_communicationButton != null)
            {
                _communicationButton.clicked += OnCommunicationClicked;
                _communicationBound = true;
                communicationTowerController?.NotifyActionButtonConnected();
            }
        }
    }

    private void UnbindButtons()
    {
        if (_placeBarrierBound && _placeBarrierButton != null)
        {
            _placeBarrierButton.clicked -= OnPlaceBarrierClicked;
            _placeBarrierButton = null;
            _placeBarrierBound = false;
        }

        if (_shelterCandidateBound && _shelterCandidateButton != null)
        {
            _shelterCandidateButton.clicked -= OnShelterCandidateClicked;
            _shelterCandidateButton = null;
            _shelterCandidateBound = false;
        }

        if (_evacuationBound && _evacuationButton != null)
        {
            _evacuationButton.clicked -= OnEvacuationClicked;
            _evacuationButton = null;
            _evacuationBound = false;
        }

        if (_preparationCardsBound && _preparationCardsButton != null)
        {
            _preparationCardsButton.clicked -= OnPreparationCardsClicked;
            _preparationCardsButton = null;
            _preparationCardsBound = false;
        }

        if (_communicationBound && _communicationButton != null)
        {
            _communicationButton.clicked -= OnCommunicationClicked;
            _communicationButton = null;
            _communicationBound = false;
        }
    }

    private void OnPlaceBarrierClicked()
    {
        TogglePlaceBarriersMode();
    }

    private void OnShelterCandidateClicked()
    {
        ToggleShelterCandidateMode();
    }

    private void OnEvacuationClicked()
    {
        ToggleEvacuationPreview();
    }

    private void OnPreparationCardsClicked()
    {
        TogglePreparationCardsUI();
    }

    private void OnCommunicationClicked()
    {
        ToggleCommunicationMode();
    }

    private void SubscribeToExternalState()
    {
        if (!_isSubscribedToBarrierMode && floodDefenseBoxStamp != null)
        {
            floodDefenseBoxStamp.ZoneBoundaryModeChanged += OnZoneBoundaryModeChanged;
            _isSubscribedToBarrierMode = true;
        }

        if (!_isSubscribedToCommunicationMode && communicationTowerController != null)
        {
            communicationTowerController.CommunicationModeChanged += OnCommunicationModeChanged;
            _isSubscribedToCommunicationMode = true;
        }

        if (!_isSubscribedToCardsUI && preparationCardsController != null)
        {
            preparationCardsController.CardsUIVisibilityChanged += OnCardsUIVisibilityChanged;
            _isSubscribedToCardsUI = true;
        }
    }

    private void UnsubscribeFromExternalState()
    {
        if (_isSubscribedToBarrierMode && floodDefenseBoxStamp != null)
        {
            floodDefenseBoxStamp.ZoneBoundaryModeChanged -= OnZoneBoundaryModeChanged;
            _isSubscribedToBarrierMode = false;
        }

        if (_isSubscribedToCommunicationMode && communicationTowerController != null)
        {
            communicationTowerController.CommunicationModeChanged -= OnCommunicationModeChanged;
            _isSubscribedToCommunicationMode = false;
        }

        if (_isSubscribedToCardsUI && preparationCardsController != null)
        {
            preparationCardsController.CardsUIVisibilityChanged -= OnCardsUIVisibilityChanged;
            _isSubscribedToCardsUI = false;
        }
    }

    private void OnZoneBoundaryModeChanged(bool isActive)
    {
        SetButtonActive(_placeBarrierButton, isActive);
    }

    private void OnCardsUIVisibilityChanged(bool isVisible)
    {
        SetButtonActive(_preparationCardsButton, isVisible);
    }

    private void OnCommunicationModeChanged(bool isActive)
    {
        SetButtonActive(_communicationButton, isActive);
    }

    private void RefreshButtonStates()
    {
        SetButtonActive(
            _placeBarrierButton,
            floodDefenseBoxStamp != null && floodDefenseBoxStamp.IsZoneBoundaryModeActive
        );

        SetButtonActive(
            _preparationCardsButton,
            preparationCardsController != null && preparationCardsController.IsCardsUIVisible
        );

        SetButtonActive(
            _evacuationButton,
            evacuationController != null && evacuationController.IsPreviewModeActive
        );

        SetButtonActive(
            _communicationButton,
            communicationTowerController != null && communicationTowerController.IsCommunicationModeActive
        );
    }

    private void SetButtonActive(Button button, bool isActive)
    {
        if (button == null)
            return;

        if (isActive)
            button.AddToClassList(ActiveButtonClassName);
        else
            button.RemoveFromClassList(ActiveButtonClassName);
    }

    private bool AreButtonsBound()
    {
        return _placeBarrierBound &&
               _shelterCandidateBound &&
               _evacuationBound &&
               _communicationBound &&
               _preparationCardsBound;
    }
}
