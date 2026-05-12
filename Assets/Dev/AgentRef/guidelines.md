# Flood Game Agentic Coding Guidelines

## General Practices
1. RECORD CHANGES: For code-changing tasks, create or update a markdown record in `flood-game\Assets\Game\Core\Architecture\Records`. After completing the changes, summarize them in 100 words or less.
2. HUNT FOR EXAMPLES: Before implementing, look for existing code that solves a similar problem. If no clear reference exists, ask the developer whether they know of one.
3. PLAN APPROPRIATELY: For substantial, risky, or architectural changes, propose a plan before editing code. For small obvious fixes, proceed with a brief explanation.
4. COMMIT CHECKPOINTS: After meaningful completed changes, remind the developer to review and commit, or offer to make a commit if requested.

## Mockups
1. If I ask you to prototype anything related to UI, first make an HTML mockup in `flood-game\Assets\Dev\AgentRef\Mockups`.
2. Name mockups after the feature or screen being explored, such as `prep-phase-resource-panel.html`.
3. Keep mockups self-contained unless there is a clear reason to share assets or scripts.
4. Use mockups to resolve layout, interaction flow, copy, visual hierarchy, and state changes before editing Unity UI code or prefabs.
5. Represent important UI states, including empty, disabled, selected, error, overflow, and active/in-progress states when relevant.
6. Prefer existing game terminology, colors, icons, and layout patterns when they are known. If uncertain, leave a short note in the mockup explaining the assumption.
7. After creating a mockup, summarize what production Unity files or systems would likely need to change if the mockup is approved.


## Event Subscription Rules

1. The subscriber is responsible for unsubscribing from events it subscribes to.
2. Subscribe in `OnEnable()` and unsubscribe in `OnDisable()` by default.
3. Use `OnDestroy()` only for listeners that must stay subscribed while disabled.
4. Avoid anonymous lambdas for event subscriptions unless the delegate is stored and can be unsubscribed.
5. Static events and global event buses must provide an explicit cleanup/reset path.
6. Events do not own subscribers and must not destroy listener objects.

## Controller Script Rules

1. Controller scripts are the public interface for systems that need to be accessed by other systems.
2. Controller scripts should be named `XController.cs`, where `X` is the system or domain they manage, such as `ModifierController.cs` or `ResourceController.cs`.
3. Other systems should interact with a system through its controller instead of directly reading or writing that system's trackers, state objects, resolvers, or runtime collections.
4. Value retrieval methods should be named `GetX`, such as `GetModifierValue()` or `GetResourceAmount()`.
5. Value-writing methods should be named `SetX`, such as `SetModifierContribution()` or `SetResourceAmount()`.
6. Use `TrySetX` when a write can fail for normal gameplay reasons and the caller needs to react to success or failure.
7. Use `CanX` methods for validation checks that do not change state, such as `CanAffordCost()` or `CanStartPrepAction()`.
8. `GetX` methods must be side-effect free. They should not mutate state, trigger gameplay events, spend resources, or initialize missing data.
9. `SetX` methods must validate inputs before changing state and should log clear errors or warnings when invalid data is passed in.
10. Controllers should enforce system invariants, such as clamping resource values, rejecting unknown modifier keys, or preventing illegal phase transitions.
11. Controllers may call lower-level scripts in the same system, such as trackers, resolvers, or runtime state classes, but those lower-level scripts should not call back into the controller.
12. Controllers should raise C# events after successful state changes when other systems need to react.
13. Controllers should not contain UI logic, save-file serialization, asset lookup, or scenario selection logic unless that is the explicit purpose of the system they control.
