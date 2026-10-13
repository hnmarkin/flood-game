# 2026-08-11 HUD Collapse Bindings

Fixed HUD collapse wiring for Alerts and Inventory Tools. Alerts now starts with its collapsed button hidden and uses the actual restore button name. Inventory Tools is registered with the reusable `CollapsiblePanelController`, including the live scene UIDocument binding. The controller now adds missing default bindings safely, warns clearly for missing UXML names, and can auto-locate a UIDocument by the full-panel element when a binding has no document assigned.
