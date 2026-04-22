// GamiTabGroup.cs  v1.2.0
// Shipped as part of: com.bravegames.gamiprefabimport (Runtime/)
// Original location: Assets/ArteditorTool/
//
// v1.2.0 — Split editor preview into separate file (Editor/GamiTabGroupEditor.cs)
//          so this script can live in a Runtime-only assembly. No behavior change.
// v1.1.0 — Original shipped version (with inline CustomEditor).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamiTabGroup : MonoBehaviour
{
    [Header("Auto-populated by ArteditorImporter")]
    public List<Button>     buttons    = new List<Button>();
    public List<GamiButton> aebButtons = new List<GamiButton>();

    [Header("Initial Selection")]
    public int defaultTab = 0;

    // ─────────────────────────────────────────────────────────────────
    void Start()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i;
            buttons[i].onClick.AddListener(() => SelectTab(idx));
        }
        // Apply initial selection immediately (no animation)
        SelectTab(defaultTab, immediate: true);
    }

    // ── Public API ────────────────────────────────────────────────────

    public void SelectTab(int index, bool immediate = false)
    {
        for (int i = 0; i < aebButtons.Count; i++)
        {
            if (aebButtons[i] == null) continue;
            aebButtons[i].SetSelected(i == index, immediate);
        }
    }
}
