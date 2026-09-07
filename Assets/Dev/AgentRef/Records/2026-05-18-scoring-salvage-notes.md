# 2026-05-18 Scoring Salvage Notes

Source review: `Assets/Game/Features/Scoring`

## Worth Salvaging

- Scoring metric names from `ScoringData.asset`
  - Homes flooded percent
  - Casualties
  - Utility downtime hours
  - Businesses affected percent
  - Economic losses
  - Infrastructure damage percent
- Persona framing from `ScoringPromptController.cs`
  - Separate resident, business, and government viewpoints
  - Structured response expectations instead of free-form output
  - Faction-specific prompt blocks
- LLM safety/robustness ideas from `LLMController.cs` and `LLMService.cs`
  - Input validation before request
  - Retry and timeout handling
  - Cancellation support
  - Structured parsing instead of trusting prose
- Environment bootstrap idea from `EnvStartup.cs`
  - Load API/environment variables before scene load

## Already Replaced By The Current LLM System

- `LLMManager.cs` in `Assets/Game/Features/LLM System`
  - Replaces the old Unity-side request orchestration
- `LLMModels.cs`
  - Replaces the old ad hoc request/response payload shapes
- `LLMPersona.cs` and persona assets
  - Replace the old scoring-only persona/prompt concept with reusable persona data
- `LLMServer/llm_server.py`
  - Owns the actual request flow for the current implementation

## Redundant Or Low Value

- `LLMController.cs`
- `LLMService.cs`
- `LLMConfig.cs`
- `LLMConfig.asset`
- `ScoringPromptController.cs`
- `EnvStartup.cs` if the current runtime no longer needs `MY_API_KEY` in Unity

## Recommendation

If this feature is removed, preserve only the metric list and the prompt/rubric ideas above in the new LLM documentation. Do not carry over the old OpenAI-style Unity networking code unless the architecture changes back to direct client-side LLM calls.

## Minimal Code References

### `ScoringData` fields

```csharp
public float homesFloodedPercent;
public int casualties;
public float utilityDowntimeHours;
public float businessesAffectedPercent;
public float economicLosses;
public float infrastructureDamagePercent;
```

### `ScoringPromptController` structure

```csharp
public string BuildResidentPrompt()
{
    promptBuilder.AppendLine(residentsDescription);
    promptBuilder.AppendLine("Here is the aftermath of the flood:");
    promptBuilder.AppendLine($"- Homes flooded: {scoringData.homesFloodedPercent}%");
    promptBuilder.AppendLine($"- Casualties: {scoringData.casualties}");
    promptBuilder.AppendLine("Example: 'Residents: 4, [rest of response]");
    return promptBuilder.ToString();
}
```

```csharp
public string BuildCorporatePrompt()
{
    promptBuilder.AppendLine(corporateDescription);
    promptBuilder.AppendLine("Here is the aftermath of the flood:");
    promptBuilder.AppendLine($"- Businesses affected: {scoringData.businessesAffectedPercent}%");
    promptBuilder.AppendLine($"- Economic losses: {scoringData.economicLosses}");
    promptBuilder.AppendLine("Example: 'Corporate: 4, [rest of response]");
    return promptBuilder.ToString();
}
```

```csharp
public string BuildPoliticalPrompt()
{
    promptBuilder.AppendLine(politicalDescription);
    promptBuilder.AppendLine("Here is the aftermath of the flood:");
    promptBuilder.AppendLine($"- Homes flooded: {scoringData.homesFloodedPercent}%");
    promptBuilder.AppendLine($"- Casualties: {scoringData.casualties}");
    promptBuilder.AppendLine($"- Businesses affected: {scoringData.businessesAffectedPercent}%");
    promptBuilder.AppendLine($"- Economic losses: {scoringData.economicLosses}");
    promptBuilder.AppendLine("Example: 'Political: 4, [rest of response]");
    return promptBuilder.ToString();
}
```

### `LLMService` request flow

```csharp
public async Task<string> AskQuestion(string question, CancellationToken cancellationToken = default)
{
    if (string.IsNullOrEmpty(question))
        throw new ArgumentException("Question cannot be empty");

    var timeSinceLastRequest = (DateTime.Now - lastRequestTime).TotalSeconds;
    if (timeSinceLastRequest < config.minRequestInterval)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(config.minRequestInterval - timeSinceLastRequest),
            cancellationToken
        );
    }

    int retryCount = 0;
    while (retryCount <= config.maxRetries)
    {
        try
        {
            return await SendRequest(question, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            if (retryCount == config.maxRetries)
                throw;

            retryCount++;
            await Task.Delay(
                TimeSpan.FromSeconds(config.retryDelay * retryCount),
                cancellationToken
            );
        }
    }

    throw new Exception("Maximum retry attempts exceeded");
}
```

### `LLMModels` payload contract

```csharp
[Serializable]
public class PersonaPayload
{
    public string personaName;
    public string personaType;
    public string tone;
    public string urgency;
    public string empathy;
    public int harshness;
    public string description;
}

[Serializable]
public class EventRequestPayload
{
    public PersonaPayload persona;
    public string eventText;
}

[Serializable]
public class EventResultPayload
{
    public string action;
    public string commentary;
}
```

### `EnvStartup` bootstrap

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void OnBeforeSceneLoad()
{
    string envPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../API_Key.env"));
    EnvLoader.LoadEnvFile(envPath);
}
```
