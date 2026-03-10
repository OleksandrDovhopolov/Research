---
name: Assets Folder Reorg
overview: Define a clear, scalable `Assets` structure by grouping runtime code, features, content, third-party assets, and project configs; then migrate folders in safe batches with asmdef and scene reference validation.
todos:
  - id: define-target-taxonomy
    content: Confirm and lock target top-level structure (`_Project`, `_ThirdParty`, `_Addressables`) and naming conventions.
    status: pending
  - id: create-move-map
    content: Create exact source-to-target folder mapping for each current top-level folder and critical child folder.
    status: pending
    dependencies:
      - define-target-taxonomy
  - id: migrate-runtime-code
    content: Move runtime code/asmdef folders in small batches and verify compile after each batch.
    status: pending
    dependencies:
      - create-move-map
  - id: migrate-content-assets
    content: Move prefabs/sprites/ui/animations/materials and validate scene/prefab references.
    status: pending
    dependencies:
      - migrate-runtime-code
  - id: migrate-config-thirdparty
    content: Relocate config/addressables/third-party folders and validate addressables + platform plugin setup.
    status: pending
    dependencies:
      - migrate-content-assets
  - id: cleanup-and-guardrails
    content: Remove empty legacy folders and add folder hygiene rules to prevent future sprawl.
    status: pending
    dependencies:
      - migrate-config-thirdparty
---

# Assets Folder Reorganization Plan

## Proposed Target Layout

- `Assets/_Project/Runtime` for game/runtime code and asmdefs currently spread across `Scripts`, `Game`, `CardCollection`, `CardsCollectionImpl`.
- `Assets/_Project/Editor` for project/editor-only utilities currently in `Editor` and editor subfolders.
- `Assets/_Project/Content` for first-party art and gameplay assets (`Prefabs`, `Sprites`, `Animations`, materials, SO configs).
- `Assets/_Project/Scenes` for scene files (`Scenes`).
- `Assets/_Project/Config` for runtime config assets/jsons (`StreamingAssets`, project-owned `Resources` assets if needed).
- `Assets/_ThirdParty` for vendor packages/plugins (`Plugins`, `Firebase`, `ExternalDependencyManager`, `TextMesh Pro`, generated Firebase repos).
- `Assets/_Addressables` for Addressables authoring/config (`AddressableAssetsData`).

## Folder Mapping (Current -> Target)

- `Assets/Scripts` -> `Assets/_Project/Runtime/App`.
- `Assets/Game` -> `Assets/_Project/Runtime/Shared` (contains infra/features/UI shared).
- `Assets/CardCollection` -> `Assets/_Project/Runtime/CardCollectionCore`.
- `Assets/CardsCollectionImpl` -> `Assets/_Project/Runtime/CardCollectionImpl` (split `Scripts` vs visual content into Runtime/Content subfolders).
- `Assets/Scenes` -> `Assets/_Project/Scenes`.
- `Assets/Prefabs`, `Assets/Sprites` -> `Assets/_Project/Content/Common`.
- `Assets/External`, `Assets/Editor`, `Assets/Editor Default Resources` -> classify into `_Project/Editor` if first-party; otherwise `_ThirdParty`.

## Recommended Rules To Keep It Clean

- Keep **one owner folder per asmdef** (runtime/editor/tests separated).
- Keep tests beside code under `Tests` with separate asmdefs.
- Disallow new top-level folders except `_Project`, `_ThirdParty`, `_Addressables`.
- Keep vendor/imported content immutable under `_ThirdParty`.
- Keep installer/bootstrap wiring in app-level runtime folder (e.g., [Scripts/GameInstaller.cs](C:/Projects/Research/Assets/Scripts/GameInstaller.cs)).

## Migration Strategy (Low Risk)

1. Freeze a target taxonomy doc and exact move map.
2. Move **code-only** folders first (asmdef-safe batches), validate compile.
3. Move content folders (`Prefabs`, `Sprites`, `Ui`, `Animations`) next; open critical scenes to verify references.
4. Move infra/config (`StreamingAssets`, `Resources`, `AddressableAssetsData`) with dedicated validation pass.
5. Remove empty/legacy folders and enforce rules for new additions.

## Validation Checklist Per Batch

- Unity compile passes (no missing script types).
- asmdef references still resolve.
- Scenes open without missing references.
- Addressables groups/build paths still valid.
- Runtime bootstrap still resolves DI/composition root.

## First Batches I’d Execute

- Batch A: `Assets/Scripts` + `Assets/Game` rehome under `_Project/Runtime`.
- Batch B: `Assets/CardCollection` + `Assets/CardsCollectionImpl/Scripts` rehome under feature runtime folders.
- Batch C: visuals/content (`Prefabs`, `Sprites`, `CardsCollectionImpl/Ui`, `CardsCollectionImpl/Sprites`, etc.).
- Batch D: platform/vendor folders under `_ThirdParty` and final cleanup.