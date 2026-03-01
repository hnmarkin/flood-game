using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private TMP_Text textLabel; // Reference to the TMP_Text component for displaying dialogue

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //GetComponent<TypewriterEffect>().Run("Hello, World!", textLabel); // Start the typewriter effect with a sample text
    }

}
