# ShelterSystem — Stage S1

Modified: `Assets/Game/Features/MitigationFeatures/Shelter/ShelterManager.cs` (declaration removal only; component body/APIs unchanged).

Created under `Assets/Dev/Dev_Shelter/Scripts/`: `PlacedShelterData.cs:6`, `ShelterTypeDefinition.cs:5`, their `.meta` files; this report.

Moved from `ShelterManager.cs:18` and `:7`, respectively. Declarations remain byte-identical, including fields/order/defaults/attributes.

Serialization: global namespace and `Assembly-CSharp` preserved; one declaration each. Manager GUID `99c9588008534c42bc36f39928818186` unchanged; new GUIDs unique.

Compilation: 202 sources, 0 errors/12 existing warnings. 4,384 protected files unchanged.

Legacy: manager, candidate controller, ResourceBarRuntimeController references and scene bindings remain.

Play Mode pending:

- Check catalogs/sprites, placement/mitigation, labels/coverage.
- Verify communication/evacuation, inactive legacy components, missing-script/type errors.
