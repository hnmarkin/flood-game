# Issue 31: Dev Game State foundation

Added a scene-independent Game State controller with authoritative Flow, Phase, and Tool State transitions, typed C# events, explicit results, ordered scenario initialization, reverse teardown, and crisis/scoring handoffs. Added public-seam EditMode coverage and documentation. Integrated the existing Dev Water coordinator through the Game State lifecycle enums and explicit crisis-time handoff, while preventing production Water progression from using Unity wall-clock `Update()`.
