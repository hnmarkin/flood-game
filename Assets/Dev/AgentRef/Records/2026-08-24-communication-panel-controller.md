# Communication Panel Controller

Implemented dynamic CommunicationPanel behavior from the design brief. Added a panel controller for tower-driven show/hide, minimize/restore, dynamic zone tab population, warning selection, status/risk icons, and send-warning updates. Wired tower activation/re-clicks to open the panel, added read-only shelter and evacuation helpers, and placed the CommunicationPanel UIDocument/controller in PlayableTilemap.

Follow-up: controller now explicitly toggles `com_panel_root`, so the UXML can stay hidden by default while tower activation restores the visible panel.
