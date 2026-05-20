using UnityEngine;

public class QuizTestTrigger : MonoBehaviour
{
    [Header("Test Trigger")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Q;

    [Header("Test Quiz Question")]
    [SerializeField] private QuizPopupController.QuizQuestionData testQuestion;

    private void Reset()
    {
        testQuestion = new QuizPopupController.QuizQuestionData
        {
            question = "What is the main purpose of placing flood barriers?",

            option1 = "To completely stop all water from entering the map",
            option2 = "To slow or redirect floodwater and reduce damage",
            option3 = "To increase water depth in nearby zones",

            correctOptionIndex = 1,

            explanationForOption1 = "Not quite. Flood barriers may reduce or redirect water flow, but they usually do not completely stop all flooding.",
            explanationForOption2 = "Correct. Flood barriers are used to slow, block, or redirect floodwater so vulnerable zones experience less damage.",
            explanationForOption3 = "Not quite. Increasing water depth is not the goal. If a barrier causes water buildup, that is usually a tradeoff the player needs to manage."
        };
    }

    private void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            if (QuizPopupController.Instance == null)
            {
                Debug.LogError("QuizTestTrigger: No QuizPopupController found in the scene.");
                return;
            }

            QuizPopupController.Instance.ShowQuiz(testQuestion);
        }
    }
}