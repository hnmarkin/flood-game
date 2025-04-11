using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class TypewriterEffect : MonoBehaviour
{
    [SerializeField] private float typingSpeed = 50f; // Speed of the typewriter effect
    public AudioSource audioSource;
    [SerializeField] private AudioClip typeSFX;
    [SerializeField] private bool sfx;

    public void Run(string textToType, TMP_Text textLabel, Action onComplete)
    {
        StartCoroutine(TypeText(textToType, textLabel, onComplete));
    }

    private IEnumerator TypeText(string textToType, TMP_Text textLabel, Action onComplete) {
        float t = 0f;
        int charIndex = 0;

        //Wait 1 second
        yield return new WaitForSeconds(1f);

        while (charIndex < textToType.Length) {
            t += Time.deltaTime * typingSpeed; // Increment time based on the typing speed
            charIndex = Mathf.FloorToInt(t);
            charIndex = Mathf.Clamp(charIndex, 0, textToType.Length);

            textLabel.text = textToType.Substring(0, charIndex);

            if (sfx)// if bool in inspector is true play sfx while text is typed
            {
                audioSource.PlayOneShot(typeSFX);
            }

            //Check for mouse click
            if (Input.GetMouseButtonDown(0)) {
                // If the user clicks, show the full text immediately
                textLabel.text = textToType;
                break; // Exit the loop
            }
            yield return null;
        }

        textLabel.text = textToType;

        // Wait for 1 second before invoking the callback
        yield return new WaitForSeconds(1f);

        onComplete?.Invoke(); // Invoke the callback when typing is complete
    }
}
