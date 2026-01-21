# Policy Selection System Setup Guide

## Overview
This system provides a universal way to track the currently selected policy card values throughout your game using a singleton PolicyManager.

## Key Components

### 1. PolicyManager
- **Location**: `Assets\_Project\UI\Scripts\Policy\PolicyManager.cs`
- **Purpose**: Singleton that maintains the current selected card data and broadcasts selection events
- **Features**:
  - Singleton pattern for global access
  - UnityEvent system for loose coupling
  - Type-safe access to all CardData properties
  - Built-in validation and error handling

### 2. PolicySelectionData
- **Purpose**: Serializable data structure that mirrors CardData properties
- **Benefits**:
  - Decouples selection system from ScriptableObject dependencies
  - Allows for modification without affecting original CardData
  - Fully serializable for saving/loading game state

### 3. Updated CardLoader
- **Changes**: OnClick method now updates PolicyManager with selected card
- **Maintains**: Original functionality for info text display
- **Adds**: Global selection tracking via PolicyManager

## Setup Instructions

### Step 1: Add PolicyManager to Scene
1. Create an empty GameObject in your scene
2. Name it "PolicyManager"
3. Add the PolicyManager component
4. The GameObject will persist across scenes automatically

### Step 2: Hook Up Events (Optional)
If you want components to automatically update when selection changes:

```csharp
private void Start()
{
    if (PolicyManager.Instance != null)
    {
        PolicyManager.Instance.OnPolicySelected.AddListener(OnPolicySelected);
    }
}

private void OnPolicySelected(PolicySelectionData selectionData)
{
    // Update your UI or game logic here
    Debug.Log($"New selection: {selectionData.cardName}");
}

private void OnDestroy()
{
    if (PolicyManager.Instance != null)
    {
        PolicyManager.Instance.OnPolicySelected.RemoveListener(OnPolicySelected);
    }
}
```

### Step 3: Access Selected Data
You can access the current selection in multiple ways:

#### Direct Property Access:
```csharp
if (PolicyManager.Instance.HasSelection())
{
    string cardName = PolicyManager.Instance.SelectedCardName;
    int money = PolicyManager.Instance.SelectedMoney;
    int actionPoints = PolicyManager.Instance.SelectedActionPoints;
}
```

#### Full Object Access:
```csharp
var currentSelection = PolicyManager.Instance.CurrentSelection;
Debug.Log($"Selected: {currentSelection.cardName}");
```

#### Get Copy for Modification:
```csharp
var selectionCopy = PolicyManager.Instance.GetSelectionCopy();
selectionCopy.money += 10; // Modify without affecting original
```

## Usage Patterns

### Pattern 1: Event-Driven UI Updates
Best for UI components that need to update when selection changes:
- Subscribe to OnPolicySelected event
- Update display automatically when selection changes
- Unsubscribe in OnDestroy to prevent memory leaks

### Pattern 2: Direct Access
Best for systems that check selection state when needed:
- Check HasSelection() before accessing data
- Use direct property access for single values
- Use CurrentSelection for multiple properties

### Pattern 3: UnityEvent Integration
For connecting to UI buttons and other Unity components:
- Set up events in the inspector
- Connect buttons to PolicyManager methods
- Use the event system for loose coupling

## Integration Examples

### With UI Buttons:
```csharp
public void OnUseSelectedPolicyButton()
{
    if (!PolicyManager.Instance.HasSelection())
    {
        Debug.LogWarning("No policy selected!");
        return;
    }
    
    var policy = PolicyManager.Instance.CurrentSelection;
    ApplyPolicyEffects(policy);
}
```

### With Game State Management:
```csharp
public void ExecuteSelectedPolicy()
{
    if (!PolicyManager.Instance.HasSelection()) return;
    
    var policy = PolicyManager.Instance.CurrentSelection;
    
    // Apply to game systems
    GameState.ModifyMoney(policy.money);
    GameState.ModifyActionPoints(policy.actionPoints);
    OpinionSystem.ModifyOpinions(policy.residentialOpinion, 
                                policy.corporateOpinion, 
                                policy.politicalOpinion);
    
    // Clear selection after use
    PolicyManager.Instance.ClearSelection();
}
```

### With Save/Load System:
```csharp
[System.Serializable]
public class GameSaveData
{
    public PolicySelectionData currentPolicy;
    // other save data...
}

public void SaveGame()
{
    var saveData = new GameSaveData();
    if (PolicyManager.Instance.HasSelection())
    {
        saveData.currentPolicy = PolicyManager.Instance.GetSelectionCopy();
    }
    // Save to file...
}

public void LoadGame(GameSaveData saveData)
{
    if (saveData.currentPolicy != null && !string.IsNullOrEmpty(saveData.currentPolicy.cardName))
    {
        PolicyManager.Instance.SelectPolicy(saveData.currentPolicy);
    }
}
```

## Benefits of This Approach

1. **Global Access**: Any script can access the current selection without complex references
2. **Event-Driven**: Components can react to selection changes automatically
3. **Decoupled**: Systems don't need direct references to each other
4. **Type Safe**: All properties are strongly typed and validated
5. **Persistent**: Singleton persists across scene changes
6. **Serializable**: Can be saved/loaded with game state
7. **Flexible**: Works with UnityEvents, direct access, and custom events

## Common Integration Points

- **Combat System**: Use action points and money costs
- **Opinion System**: Apply opinion modifiers
- **Resource Management**: Track money and action point changes
- **UI Systems**: Display current selection in various panels
- **AI Systems**: AI can access current policies for decision making
- **Analytics**: Track which policies are selected most often

## Troubleshooting

### "PolicyManager instance not found!"
- Ensure PolicyManager GameObject is in the scene
- Check that the PolicyManager component is attached
- Verify singleton initialization in Awake()

### Selection not updating
- Check that CardLoader.OnClick is being called
- Verify PolicyManager.SelectPolicy() is being called
- Ensure no exceptions are preventing the update

### Events not firing
- Confirm event listeners are properly subscribed
- Check that OnPolicySelected event is not null
- Verify listeners are not unsubscribed too early
