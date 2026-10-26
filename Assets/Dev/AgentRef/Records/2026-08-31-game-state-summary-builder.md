# Game State Summary Builder

Added `GameStateSummaryBuilder.cs` to build compact event-driven JSON snapshots from shelter, risk, flood impact, evacuation, and communication controllers. Added small read-only evacuation and communication helper APIs plus tower/warning events so the builder can avoid private state and gameplay logic. The builder also hooks risk-inspection and barrier-complete events. Friendly IDs stay stable while original IDs are preserved.
