# Assets Folder Organization

This document defines the required structure of the Unity `Assets/` folder. Its goals are to:
- Separate experimentation from shippable code
- Keep features self-contained and removable
- Prevent third-party assets from contaminating core systems
- Make pull requests predictable and reviewable

## Dev
Used for in-progress development, experiments, and prototypes.

If a feature is not ready to merge into `main`, it **must** live here.

All files in this folder must be explicitly marked as disposable:
- Prefix filenames with `Dev_`

Example:  
`LLMManager.cs` → `Dev_LLMManager.cs`

You will remove this when merging to main.

## Game
The home for completed, shippable features.

When making a pull request to merge with main:
1. Create a folder in `Game/Features` for your feature.
2. Partition your files into
    - **Content:** Assets, scenes, prefabs, etc. 
    - **Mechanics:** Scripts, configs, etc.
3. Move files accordingly:
   - Feature logic → `Game/Features/YOUR_FEATURE/`
   - Content → `Game/Content/`
4. Remove all development prefixes/suffixes.
5. Open your pull request.

## Settings
Contains build profiles and render pipeline assets.

## Third Party
Contains all third-party assets and packages.

## Tools
Contains custom debugging, diagnostics, and development utilities.
These are not gameplay features.

Tools are divided into two categories:
1. **Runtime Tools:** Used during Play Mode (e.g. debugging or logging tools)
2. **Editor Tools:** used in the Unity Editor outside of Play Mode (e.g. our custom `TileCellInspector` editor window)

Editor-only code must live under an `Editor/` subfolder.

This includes:
- Asset Store packages
- External UI frameworks
- Sprite packs, audio packs, fonts
- Third-party prefabs and materials

**WARNING**: Third-party assets **must not** be moved, modified, or copied into `Game/`, `Tools/`, or `Dev/`. Failure to comply could cause problems in:
- License compliance and attribution
- Clean upgrades and removals