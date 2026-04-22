// GamiTabGroupEditor.cs  v1.2.0
// Editor-mode inspector preview for GamiTabGroup (runtime script).
// Split out of GamiTabGroup.cs in v1.2.0 so the runtime script can live
// in a Runtime-only assembly. Behavior unchanged.
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GamiTabGroup))]
public class GamiTabGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var ntg = (GamiTabGroup)target;
        GUILayout.Space(6);
        GUILayout.Label("Preview Tab (Edit Mode)", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        for (int i = 0; i < ntg.aebButtons.Count; i++)
        {
            if (GUILayout.Button($"Tab {i}"))
            {
                Undo.RecordObject(ntg, $"GamiTabGroup Select {i}");
                ntg.SelectTab(i, immediate: true);
            }
        }
        GUILayout.EndHorizontal();
    }
}
