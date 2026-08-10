using UnityEngine;
using UnityEngine.UIElements;

public class QuizPopupController : MonoBehaviour
{
    public static QuizPopupController Instance { get; private set; }

    [System.Serializable]
    public class QuizQuestionData
    {
        [TextArea(2, 4)]
        public string question;

        public string option1;
        public string option2;
        public string option3;

        [Tooltip("Use 0 for option1, 1 for option2, 2 for option3.")]
        public int correctOptionIndex;

        [TextArea(2, 5)]
        public string explanationForOption1;

        [TextArea(2, 5)]
        public string explanationForOption2;

        [TextArea(2, 5)]
        public string explanationForOption3;
    }

    private UIDocument _uiDocument;

    private VisualElement _quizRoot;
    private VisualElement _explanationBox;

    private Label _quizQuestion;
    private Label _explanationResult;
    private Label _explanationLabel;

    private Button _option1;
    private Button _option2;
    private Button _option3;
    private Button _closeButton;
    private Button _continueButton;

    private QuizQuestionData _currentQuestion;
    private Button[] _optionButtons;

    private bool _hasAnswered;

    private void Awake()
    {
        Instance = this;

        _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument == null)
        {
            Debug.LogError("QuizPopupController: No UIDocument found on this GameObject.");
            return;
        }

        VisualElement root = _uiDocument.rootVisualElement;

        _quizRoot = root.Q<VisualElement>("quiz_root");
        _explanationBox = root.Q<VisualElement>("explanation_box");

        _quizQuestion = root.Q<Label>("quiz_question");
        _explanationResult = root.Q<Label>("explanation_result");
        _explanationLabel = root.Q<Label>("explanation_label");

        _option1 = root.Q<Button>("option1");
        _option2 = root.Q<Button>("option2");
        _option3 = root.Q<Button>("option3");

        _closeButton = root.Q<Button>("quiz_close_button");
        _continueButton = root.Q<Button>("quiz_continue_btn");

        _optionButtons = new Button[] { _option1, _option2, _option3 };

        ValidateReferences();

        _option1.clicked += () => SelectOption(0);
        _option2.clicked += () => SelectOption(1);
        _option3.clicked += () => SelectOption(2);

        _closeButton.clicked += HideQuiz;
        _continueButton.clicked += HideQuiz;

        HideQuiz();
    }

    public void ShowQuiz(QuizQuestionData questionData)
    {
        if (questionData == null)
        {
            Debug.LogWarning("QuizPopupController: Tried to show quiz, but question data was null.");
            return;
        }

        _currentQuestion = questionData;
        _hasAnswered = false;

        _quizQuestion.text = questionData.question;

        _option1.text = questionData.option1;
        _option2.text = questionData.option2;
        _option3.text = questionData.option3;

        ResetOptionVisuals();

        _explanationBox.style.display = DisplayStyle.None;
        _continueButton.style.display = DisplayStyle.None;

        _quizRoot.style.display = DisplayStyle.Flex;
    }

    private void SelectOption(int selectedIndex)
    {
        if (_hasAnswered || _currentQuestion == null)
            return;

        _hasAnswered = true;

        bool isCorrect = selectedIndex == _currentQuestion.correctOptionIndex;

        for (int i = 0; i < _optionButtons.Length; i++)
        {
            Button button = _optionButtons[i];

            button.RemoveFromClassList("correct");
            button.RemoveFromClassList("wrong");
            button.RemoveFromClassList("locked");

            if (i == _currentQuestion.correctOptionIndex)
            {
                button.AddToClassList("correct");
            }
            else if (i == selectedIndex)
            {
                button.AddToClassList("wrong");
            }
            else
            {
                button.AddToClassList("locked");
            }

            button.SetEnabled(false);
        }

        _explanationResult.text = isCorrect ? "Correct!" : "Not quite.";
        _explanationLabel.text = GetExplanation(selectedIndex);

        _explanationBox.style.display = DisplayStyle.Flex;
        _continueButton.style.display = DisplayStyle.Flex;
    }

    private string GetExplanation(int selectedIndex)
    {
        switch (selectedIndex)
        {
            case 0:
                return _currentQuestion.explanationForOption1;
            case 1:
                return _currentQuestion.explanationForOption2;
            case 2:
                return _currentQuestion.explanationForOption3;
            default:
                return "";
        }
    }

    private void ResetOptionVisuals()
    {
        foreach (Button button in _optionButtons)
        {
            button.RemoveFromClassList("correct");
            button.RemoveFromClassList("wrong");
            button.RemoveFromClassList("locked");

            button.SetEnabled(true);
        }
    }

    public void HideQuiz()
    {
        if (_quizRoot != null)
            _quizRoot.style.display = DisplayStyle.None;
    }

    private void ValidateReferences()
    {
        if (_quizRoot == null) Debug.LogError("Missing UXML element: quiz_root");
        if (_explanationBox == null) Debug.LogError("Missing UXML element: explanation_box");

        if (_quizQuestion == null) Debug.LogError("Missing UXML element: quiz_question");
        if (_explanationResult == null) Debug.LogError("Missing UXML element: explanation_result");
        if (_explanationLabel == null) Debug.LogError("Missing UXML element: explanation_label");

        if (_option1 == null) Debug.LogError("Missing UXML Button: option1");
        if (_option2 == null) Debug.LogError("Missing UXML Button: option2");
        if (_option3 == null) Debug.LogError("Missing UXML Button: option3");

        if (_closeButton == null) Debug.LogError("Missing UXML Button: quiz_close_button");
        if (_continueButton == null) Debug.LogError("Missing UXML Button: quiz_continue_btn");
    }
}