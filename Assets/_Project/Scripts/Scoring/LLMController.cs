/* JUST ADDED ASYNC FUNCTIONS. THEY DON'T WORK. RESEARCH HOW TO USE TRY CATCH BLOCKS WITH ASYNC FUNCTIONS.
CONNECTION WITH API IS SHODDY, LIKELY IN NEED OF REFACTORING/REORGANIZING*/

using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

public class LLMController : MonoBehaviour
{
    [SerializeField]
    private ScoringPromptController _scoringPromptController;
    [SerializeField] private LLMService _llmService;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _cts = new CancellationTokenSource();

        if (_llmService == null)
        {
            Debug.LogError("LLMService is not set in the LLMController!");
        }
    }

    // Call this to initiate the LLM scoring process (just for Residents here).
    public async Task<(int stars, string response)> EvaluateResidentialStars()
    {
        // 1. Build the prompt from the ScoringPromptController.
        string prompt = _scoringPromptController.BuildResidentPrompt();
        Debug.Log($"{prompt}");

        // 2. Send the prompt to the LLM and get the response.
        string residentResponse = await GetLLMResponseWithTimeoutAsync(prompt, 5000);
        if (residentResponse == null)
        {
            Debug.LogWarning("LLM response was null or timed out.");
            return(0, "LLM response was null or timed out.");
        }

        // 3. Parse the star ratings (and hidden star) from the LLM’s response.
        int stars = ParseResidentialStars(residentResponse);
        // bool hasHiddenStar = ParseResidentialHiddenStar(llmResponse);

        Debug.Log($"Residential faction stars: {stars}");
        Debug.Log($"LLM response: {residentResponse}");
        
        return (stars, residentResponse);
    }

    public async Task<(int stars, string response)> EvaluateCorporateStars()
    {
        // 1. Build the prompt from the ScoringPromptController.
        string prompt = _scoringPromptController.BuildCorporatePrompt();
        Debug.Log($"{prompt}");

        // 2. Send the prompt to the LLM and get the response.
        string corporateResponse = await GetLLMResponseWithTimeoutAsync(prompt, 5000);
        if (corporateResponse == null)
        {
            Debug.LogWarning("LLM response was null or timed out.");
            return(0, "LLM response was null or timed out.");
        }

        // 3. Parse the star ratings (and hidden star) from the LLM’s response.
        int stars = ParseCorporateStars(corporateResponse);
        // bool hasHiddenStar = ParseResidentialHiddenStar(llmResponse);

        Debug.Log($"Corporate faction stars: {stars}");
        Debug.Log($"LLM response: {corporateResponse}");
        
        return (stars, corporateResponse);
    }

    public async Task<(int stars, string response)> EvaluatePoliticalStars()
    {
        // 1. Build the prompt from the ScoringPromptController.
        string prompt = _scoringPromptController.BuildPoliticalPrompt();
        Debug.Log($"{prompt}");

        // 2. Send the prompt to the LLM and get the response.
        string politicalResponse = await GetLLMResponseWithTimeoutAsync(prompt, 5000);
        if (politicalResponse == null)
        {
            Debug.LogWarning("LLM response was null or timed out.");
            return(0, "LLM response was null or timed out.");
        }

        // 3. Parse the star ratings (and hidden star) from the LLM’s response.
        int stars = ParsePoliticalStars(politicalResponse);
        // bool hasHiddenStar = ParseResidentialHiddenStar(llmResponse);

        Debug.Log($"Political faction stars: {stars}");
        Debug.Log($"LLM response: {politicalResponse}");
        
        return (stars, politicalResponse);
    }

    private async Task<string> GetLLMResponseAsync(string prompt)
    {
        try 
        {
            return await _llmService.AskQuestion(prompt, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("LLM request was cancelled.");
            return null;
        }
    }
    private async Task<string> GetLLMResponseWithTimeoutAsync(string prompt, int timeoutMilliseconds)
    {
        if (_cts.IsCancellationRequested)
        {
            Debug.LogWarning("Previous request is still running. Cancelling it.");
            _cts.Cancel();
            _cts = new CancellationTokenSource();
        }

        var timeoutTask = Task.Delay(timeoutMilliseconds, _cts.Token);
        var llmResponseTask = GetLLMResponseAsync(prompt);

        var completedTask = await Task.WhenAny(llmResponseTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Debug.LogWarning("LLM request timed out.");
            _cts.Cancel();
            return null;
        }

        return await llmResponseTask;
    }

    private void OnDisable()
    {
        _cts?.Cancel();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts.Dispose();
    }

    /// <summary>
    /// Extract the number of stars awarded to Residents using a simple Regex.
    /// Adjust as needed to match your actual LLM output format.
    /// </summary>
    private int ParseResidentialStars(string llmText)
    {
        // Looks for something like: "Residents: 4" or "Residents: 4 stars"
        Regex residentsRegex = new Regex(@"Residents:\s*(\d)");
        Match match = residentsRegex.Match(llmText);

        if (match.Success && int.TryParse(match.Groups[1].Value, out int stars))
        {
            return stars;
        }
        
        // Default to 0 if we can't parse
        return 0;
    }

    private int ParseCorporateStars(string llmText)
    {
        // Looks for something like: "Corporate: 4" or "Corporate: 4 stars"
        Regex corporateRegex = new Regex(@"Corporate:\s*(\d)");
        Match match = corporateRegex.Match(llmText);

        if (match.Success && int.TryParse(match.Groups[1].Value, out int stars))
        {
            return stars;
        }
        
        // Default to 0 if we can't parse
        return 0;
    }

    private int ParsePoliticalStars(string llmText)
    {
        // Looks for something like: "Political: 4" or "Political: 4 stars"
        Regex politicalRegex = new Regex(@"Political:\s*(\d)");
        Match match = politicalRegex.Match(llmText);

        if (match.Success && int.TryParse(match.Groups[1].Value, out int stars))
        {
            return stars;
        }
        
        // Default to 0 if we can't parse
        return 0;
    }

//     /// <summary>
//     /// Checks for any mention of a hidden star using another Regex or string search.
//     /// </summary>
//     private bool ParseResidentialHiddenStar(string llmText)
//     {
//         // Example: "Hidden Star: YES" or "Hidden star: True"
//         return llmText.ToLower().Contains("hidden star: yes");
//     }
}
