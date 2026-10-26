using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class UITypewriterController : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 45f;
    [SerializeField, Min(0f)] private float secondsPerCharacter;
    [SerializeField, Min(0f)] private float pauseBetweenLabelsSeconds = 0.2f;
    [SerializeField] private bool clearAllLabelsBeforeSequence = true;
    [SerializeField] private bool enableRichTextDuringTyping = true;
    [SerializeField] private bool treatRichTextTagsAsZeroWidth = true;
    [SerializeField] private bool useUnscaledTime = true;

    private readonly Dictionary<Label, string> _originalTextByLabel = new Dictionary<Label, string>();
    private readonly List<Label> _activeLabels = new List<Label>();
    private readonly Dictionary<Label, string> _activeOriginals = new Dictionary<Label, string>();

    private Coroutine _typingRoutine;

    private void OnValidate()
    {
        charactersPerSecond = Mathf.Max(0f, charactersPerSecond);
        secondsPerCharacter = Mathf.Max(0f, secondsPerCharacter);
        pauseBetweenLabelsSeconds = Mathf.Max(0f, pauseBetweenLabelsSeconds);
    }

    private void OnDisable()
    {
        FinishInstantly();
    }

    public Coroutine TypeLabel(Label label)
    {
        return TypeLabel(label, charactersPerSecond, 0f);
    }

    public Coroutine TypeLabel(Label label, float overrideCharactersPerSecond, float startDelaySeconds = 0f)
    {
        if (label == null)
        {
            Debug.LogWarning("[UITypewriterController] Cannot type a null Label.");
            return null;
        }

        return TypeLabelsInSequence(
            new[] { label },
            overrideCharactersPerSecond,
            0f,
            startDelaySeconds);
    }

    public Coroutine TypeLabelsInSequence(IList<Label> labels)
    {
        return TypeLabelsInSequence(labels, charactersPerSecond, pauseBetweenLabelsSeconds, 0f);
    }

    public Coroutine TypeLabelsInSequence(
        IList<Label> labels,
        float overrideCharactersPerSecond,
        float overridePauseBetweenLabelsSeconds,
        float startDelaySeconds = 0f)
    {
        List<Label> validLabels = BuildValidLabelList(labels);

        if (validLabels.Count == 0)
        {
            Debug.LogWarning("[UITypewriterController] No valid Labels were supplied for typewriter playback.");
            return null;
        }

        StopTyping(true);
        PrepareActiveLabels(validLabels);

        float characterDelay = ResolveCharacterDelay(overrideCharactersPerSecond);
        float pauseSeconds = Mathf.Max(0f, overridePauseBetweenLabelsSeconds);
        float delayBeforeStart = Mathf.Max(0f, startDelaySeconds);

        _typingRoutine = StartCoroutine(TypeLabelsRoutine(validLabels, characterDelay, pauseSeconds, delayBeforeStart));
        return _typingRoutine;
    }

    public void Skip()
    {
        FinishInstantly();
    }

    public void FinishInstantly()
    {
        StopTyping(true);
    }

    public void StopTyping(bool completeInstantly)
    {
        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }

        if (completeInstantly)
        {
            for (int i = 0; i < _activeLabels.Count; i++)
            {
                Label label = _activeLabels[i];

                if (label == null)
                    continue;

                if (_activeOriginals.TryGetValue(label, out string originalText))
                    label.text = originalText;
            }
        }

        _activeLabels.Clear();
        _activeOriginals.Clear();
    }

    public void ClearCachedOriginals()
    {
        _originalTextByLabel.Clear();
    }

    private IEnumerator TypeLabelsRoutine(
        IList<Label> labels,
        float characterDelay,
        float pauseSeconds,
        float startDelaySeconds)
    {
        if (startDelaySeconds > 0f)
            yield return WaitForSecondsByMode(startDelaySeconds);

        for (int i = 0; i < labels.Count; i++)
        {
            Label label = labels[i];

            if (label == null)
                continue;

            string originalText = _activeOriginals.TryGetValue(label, out string text)
                ? text
                : string.Empty;

            yield return TypeLabelRoutine(label, originalText, characterDelay);

            if (pauseSeconds > 0f && i < labels.Count - 1)
                yield return WaitForSecondsByMode(pauseSeconds);
        }

        _typingRoutine = null;
        _activeLabels.Clear();
        _activeOriginals.Clear();
    }

    private IEnumerator TypeLabelRoutine(Label label, string originalText, float characterDelay)
    {
        if (label == null)
            yield break;

        if (enableRichTextDuringTyping)
            label.enableRichText = true;

        label.text = string.Empty;

        if (characterDelay <= 0f)
        {
            label.text = originalText;
            yield break;
        }

        int visibleCharacterCount = CountVisibleCharacters(originalText);

        for (int visibleCharacters = 1; visibleCharacters <= visibleCharacterCount; visibleCharacters++)
        {
            label.text = BuildVisibleText(originalText, visibleCharacters);
            yield return WaitForSecondsByMode(characterDelay);
        }

        label.text = originalText;
    }

    private void PrepareActiveLabels(IList<Label> labels)
    {
        for (int i = 0; i < labels.Count; i++)
        {
            Label label = labels[i];

            if (label == null)
                continue;

            string originalText = CaptureOriginalText(label);

            _activeLabels.Add(label);
            _activeOriginals[label] = originalText;

            if (clearAllLabelsBeforeSequence)
                label.text = string.Empty;
        }
    }

    private string CaptureOriginalText(Label label)
    {
        if (label == null)
            return string.Empty;

        string currentText = label.text ?? string.Empty;

        if (!_originalTextByLabel.TryGetValue(label, out string originalText) || !string.IsNullOrEmpty(currentText))
        {
            originalText = currentText;
            _originalTextByLabel[label] = originalText;
        }

        return originalText;
    }

    private List<Label> BuildValidLabelList(IList<Label> labels)
    {
        List<Label> validLabels = new List<Label>();

        if (labels == null)
            return validLabels;

        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] != null)
                validLabels.Add(labels[i]);
        }

        return validLabels;
    }

    private float ResolveCharacterDelay(float overrideCharactersPerSecond)
    {
        if (secondsPerCharacter > 0f)
            return secondsPerCharacter;

        float resolvedCharactersPerSecond = Mathf.Max(0f, overrideCharactersPerSecond);

        if (resolvedCharactersPerSecond <= 0f)
            return 0f;

        return 1f / resolvedCharactersPerSecond;
    }

    private int CountVisibleCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        if (!treatRichTextTagsAsZeroWidth)
            return text.Length;

        int count = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '<')
            {
                int tagEnd = text.IndexOf('>', i);

                if (tagEnd > i)
                {
                    i = tagEnd;
                    continue;
                }
            }

            count++;
        }

        return count;
    }

    private string BuildVisibleText(string sourceText, int visibleCharacterLimit)
    {
        if (string.IsNullOrEmpty(sourceText) || visibleCharacterLimit <= 0)
            return string.Empty;

        if (!treatRichTextTagsAsZeroWidth)
        {
            int characterCount = Mathf.Min(visibleCharacterLimit, sourceText.Length);
            return sourceText.Substring(0, characterCount);
        }

        int totalVisibleCharacters = CountVisibleCharacters(sourceText);

        if (visibleCharacterLimit >= totalVisibleCharacters)
            return sourceText;

        StringBuilder builder = new StringBuilder(sourceText.Length);
        List<string> openTags = new List<string>();
        int visibleCharacters = 0;

        for (int i = 0; i < sourceText.Length && visibleCharacters < visibleCharacterLimit;)
        {
            char character = sourceText[i];

            if (character == '<')
            {
                int tagEnd = sourceText.IndexOf('>', i);

                if (tagEnd > i)
                {
                    string tag = sourceText.Substring(i, tagEnd - i + 1);
                    builder.Append(tag);
                    TrackRichTextTag(tag, openTags);
                    i = tagEnd + 1;
                    continue;
                }
            }

            builder.Append(character);
            visibleCharacters++;
            i++;
        }

        for (int i = openTags.Count - 1; i >= 0; i--)
        {
            builder.Append("</");
            builder.Append(openTags[i]);
            builder.Append('>');
        }

        return builder.ToString();
    }

    private void TrackRichTextTag(string tag, List<string> openTags)
    {
        if (string.IsNullOrEmpty(tag) || tag.Length < 3 || openTags == null)
            return;

        string tagContent = tag.Substring(1, tag.Length - 2).Trim();

        if (string.IsNullOrEmpty(tagContent) || tagContent[0] == '!' || tagContent[0] == '?')
            return;

        bool isClosingTag = tagContent[0] == '/';

        if (isClosingTag)
        {
            string closingTagName = ParseTagName(tagContent.Substring(1));

            for (int i = openTags.Count - 1; i >= 0; i--)
            {
                if (openTags[i] == closingTagName)
                {
                    openTags.RemoveAt(i);
                    break;
                }
            }

            return;
        }

        if (tagContent.EndsWith("/"))
            return;

        string openingTagName = ParseTagName(tagContent);

        if (!string.IsNullOrEmpty(openingTagName) && !IsSelfClosingRichTextTag(openingTagName))
            openTags.Add(openingTagName);
    }

    private string ParseTagName(string tagContent)
    {
        if (string.IsNullOrWhiteSpace(tagContent))
            return string.Empty;

        string trimmedContent = tagContent.Trim();
        int equalsIndex = trimmedContent.IndexOf('=');
        int spaceIndex = trimmedContent.IndexOf(' ');
        int endIndex = trimmedContent.Length;

        if (equalsIndex >= 0)
            endIndex = Mathf.Min(endIndex, equalsIndex);

        if (spaceIndex >= 0)
            endIndex = Mathf.Min(endIndex, spaceIndex);

        return trimmedContent.Substring(0, endIndex).ToLowerInvariant();
    }

    private bool IsSelfClosingRichTextTag(string tagName)
    {
        return tagName == "br" || tagName == "quad" || tagName == "sprite" || tagName == "img";
    }

    private object WaitForSecondsByMode(float seconds)
    {
        return useUnscaledTime
            ? new WaitForSecondsRealtime(seconds)
            : new WaitForSeconds(seconds);
    }
}
