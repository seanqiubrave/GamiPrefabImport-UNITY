# Changelog

All notable changes to this package are documented in this file.

## [4.0.0] — 2026-04-23

### Added
- **Runtime scripts bundled**: `GamiButton.cs` (v1.4.7) and `GamiTabGroup.cs` (v1.2.0)
  now ship inside the package under `Runtime/`. Nav Button Generator ZIPs import
  cleanly on a fresh project — users no longer need to install the button tool
  separately.
- New Runtime assembly: `BraveGames.GamiPrefabImport.Runtime.asmdef`.
- Split editor-only inspector preview into `Editor/GamiTabGroupEditor.cs`.

### Changed
- Editor assembly now references the new Runtime assembly.
- Package description updated to reflect all three supported tool formats
  (Batch 9-Slice, PSD to Prefabs, Nav Button Generator).

### Breaking
- **Duplicate scripts**: if you previously copied `GamiButton.cs` or
  `GamiTabGroup.cs` into your `Assets/` folder (for example, under
  `Assets/ArteditorTool/`), delete those duplicates after updating. The
  package now provides them, and Unity will fail to compile if both exist.

## [3.3.0] — Unreleased on GitHub (rolled into 4.0.0)

### Fixed
- Removed `fontSize * 0.75f` multiplier on nav-button text (was shrinking 25%).
- Read `click_effect` (`grow` | `pop`) per-button from `layout.json` and apply
  to `GamiButton.clickEffect` at post-process bake. Default: `grow`.

## [3.2.4]

- `anchoredPosition` sourced from `rectTransform` in `layout.json`
  (fixes multi-layer positions).
- Text uses stretch anchor with padding Left=28 Top=14 Right=28 Bottom=34.
- Text content forced ToUpper() — PSD "All Caps" style preserved.
- TMP font matching via project-wide search (exact then partial match on first word).

## [3.2.3]

- Skip hidden PSD layers entirely (was `SetActive(false)`, now returns 0 / omits).

## [3.2.1]

- Text layers read `node.text` for fontSize / color / alignment / fontStyle.
- Asset lookup tries both `asset.assetId` and `asset.id`.
- 9-slice `spriteBorder` uses compact-PNG-local coordinates.

## [3.1.0]

- **New**: PSD Prefab import path (additive). Detects ZIPs produced by
  Gami PSD → Prefab (`PsdToPrefab.html`) via `"format": "gami-psd-prefab"`
  marker in `layout.json` and builds a Unity UI hierarchy that preserves
  the PSD layer tree 1:1. Existing paths unchanged.

## [3.0.1]

- Fixed Unity 2022.3 compatibility (TextMeshPro 3.0.6 API — use
  `enableWordWrapping` instead of `textWrappingMode`).

## [3.0.0]

- **Free Edition**. Removed all account / credit / payment / network logic.
  Pure local ZIP-to-Prefab converter. No network calls. No tracking.
