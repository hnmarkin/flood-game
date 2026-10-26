# Stage 1 UI navigation

This feature adds one shared UIDocument for Actions, Preparation Cards, and drainage Extra Info. All existing Game UXML/USS assets remain independent and unchanged. No scene YAML was edited.

## Files

- `Scripts/UIManager.cs`: `FloodGame.UI.Navigation.UIManager`, the public navigation gateway and internal three-panel registry/stack.
- `Scripts/UIPanelId.cs`: panel identifiers and the typed `PreparationInfoId.DrainageMaintenanceBlitz` identifier.
- `Scripts/PreparationCardsPanelController.cs`: drainage info and the existing Preparation close button.
- `Scripts/ExtraInfoPanelController.cs`: the existing header X and footer CLOSE buttons; drainage content stays in UXML.
- `UI/Dev_SharedUIRoot.uxml` and `.uss`: empty MainLayer/PopupLayer containers, template host positioning.
- Modified `Assets/Dev/Scripts/PanelController/ActionsPanelController.cs`: own-subtree binding, manager navigation, visibility styling, and legacy handoff.

Popup controllers are ordinary C# objects created/disposed by UIManager. **Do not add them as GameObject components.** The legacy global `UIManager` in `Dev/Old_UI_Ref` is untouched; use the namespaced Stage 1 component below.

The new scripts keep the names explicitly requested in the task; the new root and guide use the repository's `Dev_` filename convention. The existing shared controller is the sole focused shared-folder edit requested by the task.

## Inspected element mapping

| Asset | Actual element/type | Usage |
| --- | --- | --- |
| ActionsPanel | `actions_root` / VisualElement | Root injected into ActionsPanelController. |
| ActionsPanel | `action_Button1`, `action_Button2`, `action_Button5`, `action_Button6` / Button | Existing barrier, shelter, evacuation, communication bindings remain. |
| ActionsPanel | `action_Button3` / Button | Still owned by HighRiskManager, through the same UIDocument reference. |
| ActionsPanel | `action_Button4` / Button | Still owned by FloodImpactOverlayManager, through the same UIDocument reference. |
| ActionsPanel | `action_Button7` / Button | Toggle Preparation Cards. `action-button-active` follows manager visibility. |
| ActionsPanel | `actions_full_panel`, `actions_minimize_btn`, `actions_collapsed_btn` | Existing CollapsiblePanelController names/references remain. |
| Preparation_Cards | `prep_cards_root > pc_container > pc_body > preparation_cards > pc_row_1 > drainage_action_card` | Drainage card is a **Button**, not a passive container. |
| Preparation_Cards | `drainage_action_card > card_header > info_btn` / Button | Scoped drainage info query. The actual name is **info_btn**, repeated on all four cards. |
| Preparation_Cards | `pc_header > [unnamed VisualElement] > [unnamed Button] > Label("X")` | Existing Preparation close button; queried only inside `pc_header`. |
| Preparation_Cards | `pump_stations_card`, `detention_card`, `levee_card` / Button | Unwired in this stage. Their repeated `info_btn` buttons are also unwired. |
| Preparation_Cards | `pc_footer > [unnamed Button] > Label("CONFIRM SELECTION")` | Unwired in this stage. |
| Extra_Info_Panel | `info_panel_root > info_container` | Existing drainage information, including all authored labels and images. |
| Extra_Info_Panel | `info_header > [unnamed VisualElement] > Button(text="X")` | Close Extra Info only. |
| Extra_Info_Panel | `info_footer > [unnamed VisualElement] > Button(text="CLOSE")` | Close Extra Info only. |
| Legacy PreparationCardsUI | `prep_card_root > prep_card_container > prep_card_scroll > prep_card_grid` | Remains owned by legacy PreparationCardsController; no migration of its JSON-generated actions. |

Both new popup templates already have close buttons, so no close controls or element names were added. Extra Info's associated USS is empty and is not referenced by its UXML; its authored appearance is inline. Preparation_Cards USS is also empty. ActionsPanel retains its existing USS hover/active classes and inline layout.

## Exact Unity Inspector setup

1. Open `Assets/Game/Content/Scenes/PlayableTilemap.unity`. For validation, use **File > Save As** to create `Assets/Dev/Dev_UINavigation/Scenes/Dev_UINavigation.unity` (create the Scenes folder in the Editor). Perform the following Inspector changes in this Dev scene. Keep the original Game scene unchanged.
2. Select **UI > ActionList > UIDocument**. Reuse its existing **UI Document** component. Change **Source Asset** from `ActionsPanel.uxml` to `Assets/Dev/Dev_UINavigation/UI/Dev_SharedUIRoot.uxml`.
3. Keep **Panel Settings** assigned to `Assets/UI Toolkit/PanelSettings.asset`. Use **Sorting Order = 1**, matching the former Preparation overlay's order and placing this root above ordinary HUD documents at order 0. Keep the current PanelSettings scale/resolution (800 x 640 reference resolution). AdvisorBigPanel is also order 1; its relationship to these popups remains a manual stacking check.
4. On this **same GameObject**, add **Flood Game > UI > Navigation > UI Manager (Stage 1)**. Do not add the legacy global UIManager.
5. Assign its Inspector fields:

   | Field | Assignment |
   | --- | --- |
   | Actions Panel Template | `Assets/Game/Features/UI/UI_Documents/UI_HUD/ActionsPanel.uxml` |
   | Preparation Cards Template | `Assets/Game/Features/UI/UI_Documents/PreparationCards/Preparation_Cards.uxml` |
   | Extra Info Template | `Assets/Game/Features/UI/UI_Documents/PreparationCards/Extra_Info_Panel.uxml` |
   | Actions Panel Controller | Existing **Systems > ActionsController** component |

6. Select **Systems > ActionsController**. Keep **Actions UI Document** pointing to the reused **UI > ActionList > UIDocument**. Assign its new **UI Manager** field to the Stage 1 manager from step 4. Keep all gameplay references and the old Preparation Cards Controller reference; the old reference is retained for unmigrated scenes and ignored after manager handoff. UIManager also injects its subtree at initialization.
7. Keep these other existing references pointing to that **same** UIDocument: **Systems > ZoneRisk > HighRiskZones** (`HighRiskManager.actionsUIDocument`), **Systems > FloodController > FloodDamageUIManager** (`FloodImpactOverlayManager.actionsUIDocument`), **UI > UI_GameFlow_Controller** (`UIVisibilityGroupController.actionsPanelDocument`), and the Actions entry on **Systems > CollapsableController** (`CollapsiblePanelController.panelUIDocument`). Do not migrate their gameplay code.
8. Add **no** separate UIDocument for ActionsPanel, Preparation_Cards, or Extra_Info_Panel. The manager clones the three templates into the reused document. There should be one live `actions_root` in that shared root.
9. Validate the new navigation path first. Then deactivate both legacy-only GameObjects in the Dev scene: **UI > PreparationCardsPanel** and **Systems > PrepCardController**. Each currently has its own **PreparationCardsController + UIDocument** using `PreparationCardsUI.uxml`. The first uses PanelSettings/order 1 and is the component referenced by ActionsController; the second has no PanelSettings. Do not delete either component or the legacy assets. Scene/component-reference inspection found no other caller for these two controllers.
10. Save the Dev scene. Applying a validated scene migration to the Game scene is a separate manual step outside this implementation's write scope.

No manager is auto-discovered globally. Before migration, ActionsPanelController still uses its existing legacy preparation route when its manager reference is absent. Once the manager is assigned/injects the root, the old visibility subscription is removed and all Preparation navigation uses the manager. A misconfigured assigned manager reports an error instead of reopening legacy UI alongside the new panels.

## Navigation behavior and lifecycle

`OpenPanel`, `ClosePanel`, `TogglePanel`, `GetPanelOpen`, `GetPanelVisibility`, and `TryGetPanelRoot` are the public manager API. `PanelVisibilityChanged(UIPanelId, bool)` reports changes after the visual state is updated. No global bus or static singleton was added.

- Preparation Cards opens in PopupLayer with a 112-pixel left inset to keep the existing Actions sidebar reachable. Its 600-pixel authored width and all internal styling/content remain intact.
- Extra Info requires `OpenPanel(UIPanelId.ExtraInfo, PreparationInfoId.DrainageMaintenanceBlitz)`. Other enum values/absent context are rejected. Opening it directly first opens Preparation Cards beneath it.
- Only the top popup is displayed/enabled. Preparation's retained instance is hidden while Extra Info is active, preserving its state. Extra Info has a full-root picking surface, and MainLayer is disabled until Extra Info closes. Parent drainage actions receive neither the info button's bubbled ClickEvent nor its consumed pointer/submit interactions.
- Preparation's active action-button style is off while that popup is hidden by Extra Info, and returns when Extra Info closes. `GetPanelOpen(PreparationCards)` remains true throughout; `GetPanelVisibility` reflects which popup is displayed.
- Either Extra Info close button reveals the same Preparation instance and restores focus. Closing Preparation also removes any descendant Extra Info entry. Actions remains rendered.
- Duplicate opens are no-ops. Closing/reopening popups reuses their instances. Manager disable closes popups and unsubscribes their controllers; re-enable rebinds to retained trees. UIDocument recreation remounts the same instances so unrelated controllers keep valid cached buttons. Manager destruction removes only its owned template hosts and releases the injected Actions root.

## Compatibility and limits

- Drainage's main action, other cards, and Confirm Selection remain unwired, as requested. Legacy JSON card availability and the legacy Sandbag card's barrier shortcut are not migrated. The existing Actions barrier/shelter/evacuation/communication routes remain unchanged.
- Stage 1 does not route intro/score phase transitions. UIVisibilityGroupController continues controlling `actions_root`; it does not control PopupLayer. Close open popups through UIManager when integrating future phase navigation.
- Higher/same-order unrelated UIDocuments can overlap this shared root. Check AdvisorBigPanel and existing intro/score overlays in the Dev scene; broader document consolidation is outside this stage.
- Existing authored UXML resource URLs include historical paths. Their GUIDs were retained; verify font, image, and USS resolution in Unity. No production asset URLs were rewritten.
- Fixed panel widths need visual verification at your supported resolutions, particularly below the current reference width.

## Verification

Executed: Unity's installed Roslyn compiler against its existing runtime compilation response file plus the four new scripts (**199 C# sources**), followed by the existing Editor assembly (**8 C# sources**) against the resulting runtime reference assembly. Both final compilations passed; the runtime assembly emitted 12 warnings from unchanged code, and the Editor assembly emitted none. Outputs were written to a temporary folder, not the project's Library. These checks use existing compiled dependency assemblies; they are not a fresh Unity player build or a Play Mode run.

Passed static checks: exact roots and scoped drainage info/close-button queries; shared root has no statically duplicated template; relative stylesheet exists; Unity metadata exists for every new asset/folder; all prior public Actions methods remain; 17 non-navigation method bodies are unchanged; Actions has no dependency on the new popup controllers; change record is 96 words. The focused Actions diff passed `git diff --check`. The repository-wide check found pre-existing trailing whitespace in the already-modified `UserSettings/Layouts/default-6000.dwlt`; that file was left untouched. The existing Game edits/deletions and other user changes remain as found at task start.

**Unity rendering, picking, keyboard navigation, and Play Mode have not been tested by the agent.** Run this checklist after Inspector setup:

1. Enter the gameplay state. In UI Toolkit Debugger, confirm one `actions_root`, one MainLayer/PopupLayer pair, and one instance of each popup template. Check original font/images, hover animation, active styling, and Actions minimize/restore.
2. Click `action_Button7`: Preparation opens and the button becomes active. Click it again outside the popup: Preparation closes, Actions remains functional. Repeat to catch duplicate callbacks.
3. Reopen Preparation and click drainage's nested `info_btn`, including its icon. Extra Info opens; the parent card's main action and other cards remain silent. Verify pointer and keyboard Submit activation both use the info route. Other cards' info buttons do nothing in this stage.
4. Click positions of the obscured Preparation cards/close button and Actions buttons while Extra Info is active. Confirm underlying controls do not fire. Close via header X, then repeat with footer CLOSE. Preparation returns in its prior state and focus returns to the previous control.
5. Close Preparation using the existing header X. Confirm Actions remains rendered and button 7 loses its active class.
6. Check Actions 1/2/5/6 gameplay behavior and 3/4 external bindings. Check intro visibility, collapse/restore, and unrelated UI overlays.
7. Disable/re-enable the manager, then the reused UIDocument, and reopen the flow. Verify popup instances/callback counts do not accumulate and Actions 3/4 retain their bindings.
8. Review Unity Console for compile/import errors, confirm both legacy popup GameObjects are inactive after validation, then review and commit the Dev changes.

API reference used during implementation: [Unity 6 UIDocument documentation](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/uielements/uidocument) and [Unity's 6000.0 Clickable reference implementation](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0/Modules/UIElements/Core/Clickable.cs).
