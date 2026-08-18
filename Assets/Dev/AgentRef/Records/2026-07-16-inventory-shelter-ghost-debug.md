# Inventory Shelter Ghost Debug

Hardened `inventory_tool_2` activation by attaching `InventoryToolsController` to the InventoryTools UIDocument, registering trickle-down click and pointer-down callbacks, and ignoring child picking inside the slot. Updated shelter ghost activation to instantiate the prefab at runtime, prepare all child SpriteRenderers, apply sorting/color/sprite fallbacks, and expose candidate-tile helper methods.
