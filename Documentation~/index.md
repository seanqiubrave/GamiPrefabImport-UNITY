# Gami Prefab Importer — Documentation

## Overview

Gami Prefab Importer is a free Unity editor tool that converts ZIP exports from [arteditor.art](https://arteditor.art) into Unity UI prefabs with proper 9-slice sprites, Button components, and TextMeshPro labels.

## Workflow

1. **Design in browser** — drop button images onto arteditor.art, set 9-slice borders, export as Unity ZIP.
2. **Import in Unity** — open `Tools > Gami Prefab Importer`, browse to your ZIP, click Import.
3. **Use the prefabs** — drag from `Assets/GamiPrefabImport/Buttons/` into any Canvas.

## Output Locations

| What                      | Where                                   |
|---------------------------|-----------------------------------------|
| Generated button prefabs  | `Assets/GamiPrefabImport/Buttons/`      |
| Sliced sprite assets      | `Assets/GamiPrefabImport/Sprites/`      |
| Composite layout prefab   | `Assets/GamiPrefabImport/layout_export.prefab` |
| Import log                | `Assets/GamiPrefabImport/import_log.txt` |

## Supported ZIP Formats

The importer auto-detects the ZIP format:

- **Single button ZIP** (e.g. `Button_Blue_ui.zip`) — produces one prefab.
- **Bundle ZIP** (e.g. `button_prefabs_unity.zip`) — produces one prefab per inner ZIP.
- **GamiPrefabEditor layout ZIP** — produces a composite prefab with multiple buttons arranged in a `HorizontalLayoutGroup`.

## Prefab Structure

Each button prefab uses:
- A root `RectTransform` sized to 370×N (auto-scaled from source aspect ratio).
- An `Image` component with the button background sprite, set to **Sliced** if 9-slice borders were detected.
- A `Button` component with Color Tint transition (Normal/Highlighted/Pressed/Disabled).
- A child `Text (TMP)` GameObject using `TextMeshProUGUI` with auto-sizing (18-75pt).

## Privacy

This package makes **no network calls** and **collects no data**. All processing is local.

The only outbound action is the optional **Open Website** button, which opens `https://arteditor.art` in your default browser.

## Support

- Website: https://arteditor.art
- Email: immortalsapp@gmail.com
- License: MIT
