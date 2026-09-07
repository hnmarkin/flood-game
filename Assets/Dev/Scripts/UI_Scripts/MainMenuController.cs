using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string gameplaySceneName = "PlayableTilemap";

    private Button startButton;
    private Button mainMenuButton;
    private Button quitButton;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("MainMenuController: No UIDocument found.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        startButton = root.Q<Button>("start_btn");
        mainMenuButton = root.Q<Button>("main_menu_btn");
        quitButton = root.Q<Button>("quit_game_btn");

        if (startButton != null)
            startButton.clicked += OnStartClicked;
        else
            Debug.LogError("Could not find button: start_btn");

        if (mainMenuButton != null)
            mainMenuButton.SetEnabled(false);

        if (quitButton != null)
            quitButton.clicked += OnQuitClicked;
        else
            Debug.LogError("Could not find button: quit_game_btn");
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.clicked -= OnStartClicked;

        if (quitButton != null)
            quitButton.clicked -= OnQuitClicked;
    }

    private void OnStartClicked()
    {
        SceneManager.LoadScene(gameplaySceneName);
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