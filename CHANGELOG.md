# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.1] - 2026-04-19

### Fixed
- Compatibility with TextMeshPro 3.0.6 (Unity 2022.3 LTS).
  Replaced TMP 4.x-only `textWrappingMode = TextWrappingModes.NoWrap`
  with the broadly supported `enableWordWrapping = false`.

## [3.0.0] - 2026-04-18

### Changed
- **Free Edition release.** All features unlocked. No account, no credits, no payment.
- Restructured as a UPM-compliant package with `package.json`, asmdef, and `Samples~`.
- Code wrapped in `BraveGames.GamiPrefabImport.Editor` namespace.
- Renamed publisher from "Brave Mobiles" to "Brave Games".

### Removed
- Account / login system.
- Credit tracking and daily limits.
- Local HTTP listener for OAuth callback.
- All network calls (the package now runs fully offline).
- `EditorPrefs` storage of device IDs and tokens.

### Added
- Asmdef for proper assembly isolation.
- TextMeshPro listed as an explicit package dependency.
- Bundled sample scene (importable via Package Manager > Samples).

## [2.0.3] - Earlier release
- Fixed anonymous credit display.

## [2.0.2]
- Fixed NullReferenceException / GUILayout mismatch.

## [2.0.1]
- Bigger, darker fonts. Fixed login flow.

## [2.0.0]
- Unified single-panel design (no tabs).

## [1.9.0]
- Auto-login via local HTTP callback.

## [1.8.3]
- HttpClient replaces WebClient (fixes 308 redirect).
