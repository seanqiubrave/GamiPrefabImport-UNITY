# Gami Prefab Importer

**Convert button images into Unity UI Prefabs in seconds.**

A free Unity editor tool that takes ZIP exports from [arteditor.art](https://arteditor.art) and turns them into ready-to-use Unity UI prefabs with proper 9-slice sprites, Button components, and TextMeshPro labels.

100% free. No account required. No network calls. No tracking.

— by Brave Games

---

## Requirements

- Unity 2022.3 LTS or later (also works with Unity 2023 and Unity 6)
- TextMeshPro (auto-installed via package dependency)

---

## Installation

### Option A — Install via Git URL (recommended)

1. In Unity, open **Window > Package Manager**.
2. Click the **`+`** button (top-left) → **Add package from git URL...**
3. Paste this URL:

   ```
   https://github.com/seanqiubrave/GamiPrefabImport-UNITY.git
   ```

4. Click **Add**. Unity will download and install the package automatically.

### Option B — Unity Asset Store

1. Search for "Gami Prefab Importer" on the [Unity Asset Store](https://assetstore.unity.com).
2. Click **Add to My Assets**, then **Open in Unity**.
3. Click **Import** in the Package Manager window.

*(Asset Store listing coming soon — currently in review.)*

### Option C — Install from disk (manual download)

1. Download this repository as a ZIP: click the green **Code** button at the top of this page → **Download ZIP**.
2. Extract the ZIP somewhere outside your Unity project (e.g. Desktop).
3. In Unity, open **Window > Package Manager**.
4. Click the **`+`** button → **Install package from disk...**
5. Browse to the extracted folder and select the `package.json` file.

---

## Quick Start

**Step 1.** Open [arteditor.art](https://arteditor.art) in Chrome (or any browser).
- Drop your button PNG or WebP images onto the page.
- Wait for thumbnail previews to appear.

**Step 2.** Click **Auto-slice All** to detect 9-slice borders.
- Drag the green guide lines to fine-tune if needed.

**Step 3.** Click **Export Unity ZIP**.
- 1 image  → downloads `FileName_ui.zip`
- 2+ images → downloads `button_prefabs_unity.zip`

**Step 4.** In Unity: open **Tools > Gami Prefab Importer**.
- Click **Browse** and select your ZIP.
- Click **Import and Generate Prefab**.

Your prefabs are saved to `Assets/GamiPrefabImport/Buttons/`.

---

## Try the Sample

After installing the package:

1. Open **Window > Package Manager**.
2. Switch to **In Project** from the left dropdown.
3. Find **Gami Prefab Importer** in the list.
4. Expand the **Samples** section on the right.
5. Click **Import** next to "Basic UI Demo".

The sample will be copied to `Assets/Samples/Gami Prefab Importer/<version>/Basic UI Demo/`. Open `ExampleScene.unity` to see two example buttons (PLAY and EXIT).

> **First time using TextMeshPro?** Unity will prompt you to "Import TMP Essentials" when you open the sample scene. Click that button — it's a one-time setup that installs the default font required for any TMP text to display.

---

## What Each Prefab Contains

```
Button_Blue      (RectTransform, Image Sliced, Button)
└── Text (TMP)   (TextMeshPro label, auto-sized, centered)
```

Button color tint (Color Tint transition, 0.08s fade):

| State       | Color (RGBA)              |
|-------------|---------------------------|
| Normal      | (1.00, 1.00, 1.00, 1.00)  |
| Highlighted | (0.92, 0.92, 0.92, 1.00)  |
| Pressed     | (0.70, 0.70, 0.70, 1.00)  |
| Disabled    | (0.50, 0.50, 0.50, 0.50)  |

---

## Using Prefabs in Your Scene

- Drag a prefab from `Assets/GamiPrefabImport/Buttons/` into a Canvas.
- Select the **Text (TMP)** child to change the button label.
- Wire a click: select the prefab, **Button** component > **On Click ()** > `+` > choose method.
- Resize freely in **RectTransform** — 9-slice keeps corners sharp.
- Uncheck **Interactable** on the Button to disable (auto-dims via color).

---

## Troubleshooting

**"TextMesh Pro Essential Resources are missing" red error**
Go to **Window > TextMeshPro > Import TMP Essential Resources** → click **Import TMP Essentials**.

**"layout.json not found in ZIP"**
Re-export from arteditor.art. Make sure thumbnails fully loaded before clicking Export.

**Button not responding to clicks**
Add an EventSystem to your scene: **GameObject > UI > Event System**.

**ZIP file is 22 bytes / empty**
Re-export after thumbnails have fully appeared in the list.

**Prefabs look wrong after import**
Enable **Debug log** in the importer window and re-run the import. The log shows which sprites were matched and any 9-slice issues.

---

## Privacy

This package does **not** collect any data, send any analytics, or make any network calls. All processing happens locally on your machine.

The only outbound action is the optional **Open Website** button, which opens `https://arteditor.art` in your default browser.

---

## Files Included

```
GamiPrefabImport/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE.md
├── Documentation~/
│   └── index.md
├── Editor/
│   ├── GamiPrefabImporter.cs
│   └── BraveGames.GamiPrefabImport.Editor.asmdef
└── Samples~/
    └── BasicUIDemo/
        ├── .sample.json
        ├── ExampleScene.unity
        └── SampleButtons/
            ├── Gami Tile Blue.prefab
            ├── Gami Tile Red.prefab
            └── (sprite assets)
```

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

---

## Support

- Website: [arteditor.art](https://arteditor.art)
- Email: immortalsapp@gmail.com
- Issues: [GitHub Issues](https://github.com/seanqiubrave/GamiPrefabImport-UNITY/issues)

---

© Brave Games. Released under the [MIT License](LICENSE.md).
