# ShelterSystem S2

Modified: `Assets/Dev/Scripts/Main_UI/UI_Overlay/Dev_ResourceBarRuntime/Scripts/ResourceBarRuntimeController.cs`.

`ShelterManager.OnShelterPlaced` → ShelterController's `public event Action<PlacedShelterData> OnShelterPlaced;` → unchanged handler/cooldown.

Resolution: Inspector/colocated component/unique active same-scene lookup (once). Missing/ambiguous: warn once. AfterSceneLoad unchanged.

OnEnable/OnDisable balanced; Update/RefreshNow detach remembered sources during destruction/reassignment, without searches. Late controllers need explicit assignment.

Removed legacy field: no saved components/assignments (845 assets).

Success: ≤1 turn; failure/cancellation: zero. Subscriptions/APIs/HUD preserved.

Compile: 202 sources, 0 errors/12 existing warnings; 27 stand-in assertions passed; 6,108 files preserved.

Remaining: candidate→manager; legacy components in both scenes.

Play Mode pending:

- Both scenes: Inspector assignment/bootstrap, success/failure/cancellation, cooldown/zero-cooldown.
- Re-enable, replace/destroy controllers, missing/ambiguous warnings; communication/evacuation turns; HUD.
