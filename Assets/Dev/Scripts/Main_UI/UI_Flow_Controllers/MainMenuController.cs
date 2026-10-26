using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    private const string MainRootName = "main_root";
    private const string PhaseSelectionHostName = "phase_selection_instance";
    private const string PhaseSelectionRootName = "phase_selection_root";
    private const string MainStartButtonName = "start_btn";
    private const string FloodCrisisButtonName = "flood_crisis_btn";
    private const string PhaseStartButtonName = "start_btn";
    private const string BackButtonName = "back_btn";

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string gameplaySceneName = "PlayableTilemap";
    [SerializeField] private bool loadPhaseSelectionScene;
    [SerializeField] private string phaseSelectionSceneName;

    private readonly Color _selectedPhaseTint = new Color(1f, 0.86f, 0.35f, 1f);
    private readonly Color _selectedPhaseBackground = new Color(1f, 0.72f, 0.18f, 0.18f);
    private readonly Color _normalButtonTint = Color.white;
    private readonly Color _disabledButtonTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    private VisualElement _mainRoot;
    private VisualElement _phaseSelectionHost;
    private VisualElement _phaseSelectionRoot;
    private Button _mainStartButton;
    private Button _phaseFloodCrisisButton;
    private Button _phaseStartButton;
    private Button _phaseBackButton;
    private Button _settingsButton;
    private Button _quitButton;
    private bool _isFloodCrisisSelected;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("MainMenuController: No UIDocument found.");
            return;
        }

        BindUI();
    }

    private void OnDisable()
    {
        if (_mainStartButton != null)
            _mainStartButton.clicked -= OnMainStartClicked;

        if (_phaseFloodCrisisButton != null)
            _phaseFloodCrisisButton.clicked -= OnFloodCrisisClicked;

        if (_phaseStartButton != null)
            _phaseStartButton.clicked -= OnPhaseStartClicked;

        if (_phaseBackButton != null)
            _phaseBackButton.clicked -= OnPhaseBackClicked;

        if (_quitButton != null)
            _quitButton.clicked -= OnQuitClicked;
    }

    private void BindUI()
    {
        VisualElement root = uiDocument.rootVisualElement;

        _mainRoot = root.Q<VisualElement>(MainRootName);
        _phaseSelectionHost = root.Q<VisualElement>(PhaseSelectionHostName);
        _phaseSelectionRoot = root.Q<VisualElement>(PhaseSelectionRootName);

        _mainStartButton = _mainRoot != null
            ? _mainRoot.Q<Button>(MainStartButtonName)
            : root.Q<Button>(MainStartButtonName);

        _settingsButton = _mainRoot?.Q<Button>("settings_btn") ?? root.Q<Button>("main_menu_btn");
        _quitButton = _mainRoot?.Q<Button>("exit_btn") ?? root.Q<Button>("quit_game_btn");

        if (_phaseSelectionRoot != null)
        {
            _phaseFloodCrisisButton = _phaseSelectionRoot.Q<Button>(FloodCrisisButtonName);
            _phaseStartButton = _phaseSelectionRoot.Q<Button>(PhaseStartButtonName);
            _phaseBackButton = _phaseSelectionRoot.Q<Button>(BackButtonName);
        }

        if (_mainStartButton != null)
            _mainStartButton.clicked += OnMainStartClicked;
        else
            Debug.LogError("MainMenuController: Could not find MainMenu/start_btn.");

        if (_phaseFloodCrisisButton != null)
            _phaseFloodCrisisButton.clicked += OnFloodCrisisClicked;
        else if (_phaseSelectionRoot != null)
            Debug.LogError("MainMenuController: Could not find PhaseSelection/flood_crisis_btn.");

        if (_phaseStartButton != null)
            _phaseStartButton.clicked += OnPhaseStartClicked;
        else if (_phaseSelectionRoot != null)
            Debug.LogError("MainMenuController: Could not find PhaseSelection/start_btn.");

        if (_phaseBackButton != null)
            _phaseBackButton.clicked += OnPhaseBackClicked;

        //if (_settingsButton != null)
          //  _settingsButton.SetEnabled(false);

        if (_quitButton != null)
            _quitButton.clicked += OnQuitClicked;
        else
            Debug.LogError("MainMenuController: Could not find exit_btn or quit_game_btn.");

        ButtonPopupManipulator.ApplyTo(_mainStartButton);
        ButtonPopupManipulator.ApplyTo(_phaseFloodCrisisButton);
        ButtonPopupManipulator.ApplyTo(_phaseStartButton);

        ShowMainMenu();
    }

    private void OnMainStartClicked()
    {
        if (loadPhaseSelectionScene)
        {
            LoadSceneByName(phaseSelectionSceneName, "Phase Selection");
            return;
        }

        if (_phaseSelectionRoot == null)
        {
            Debug.LogError("MainMenuController: Phase Selection UI was not found in the loaded Main Menu document.");
            return;
        }

        ShowPhaseSelection();
    }

    private void OnFloodCrisisClicked()
    {
        SetFloodCrisisSelected(true);
    }

    private void OnPhaseStartClicked()
    {
        if (!_isFloodCrisisSelected)
            return;

        LoadGameplayScene();
    }

    private void OnPhaseBackClicked()
    {
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        SetElementDisplay(_mainRoot, DisplayStyle.Flex);
        SetElementDisplay(_phaseSelectionHost, DisplayStyle.None);
        SetElementDisplay(_phaseSelectionRoot, DisplayStyle.None);
        SetFloodCrisisSelected(false);
    }

    private void ShowPhaseSelection()
    {
        SetElementDisplay(_mainRoot, DisplayStyle.None);
        SetElementDisplay(_phaseSelectionHost, DisplayStyle.Flex);
        SetElementDisplay(_phaseSelectionRoot, DisplayStyle.Flex);
        SetFloodCrisisSelected(false);
    }

    private void SetFloodCrisisSelected(bool isSelected)
    {
        _isFloodCrisisSelected = isSelected;

        if (_phaseFloodCrisisButton != null)
        {
            _phaseFloodCrisisButton.EnableInClassList("phase-option-selected", isSelected);
            _phaseFloodCrisisButton.style.backgroundColor = isSelected ? _selectedPhaseBackground : Color.clear;
            _phaseFloodCrisisButton.style.unityBackgroundImageTintColor = isSelected ? _selectedPhaseTint : _normalButtonTint;
        }

        if (_phaseStartButton != null)
        {
            _phaseStartButton.SetEnabled(isSelected);
            _phaseStartButton.style.unityBackgroundImageTintColor = isSelected ? _normalButtonTint : _disabledButtonTint;
        }
    }

    private void LoadGameplayScene()
    {
        LoadSceneByName(gameplaySceneName, "gameplay");
    }

    private void LoadSceneByName(string sceneName, string sceneLabel)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"MainMenuController: Cannot load {sceneLabel} because no scene name is assigned.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private static void SetElementDisplay(VisualElement element, DisplayStyle displayStyle)
    {
        if (element != null)
            element.style.display = displayStyle;
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
