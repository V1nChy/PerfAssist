using UnityEngine;
using System.Collections;
using UnityEditor;

public class MemConfigPopup : PopupWindowContent
{
    public bool AutoSaveOnSnapshot { get { return EditorPrefs.GetBool(MemPrefs.AutoSaveOnSnapshot); } }
    bool _autoSaveOnSnapshot;

    public bool DiffHideIdentical { get { return EditorPrefs.GetBool(MemPrefs.Diff_HideIdentical); } }
    bool _diffHideIdentical;

    public bool DiffHideRemoved { get { return EditorPrefs.GetBool(MemPrefs.Diff_HideRemoved); } }
    bool _diffHideRemoved;

    public MemConfigPopup()
    {
        
    }

    public override void OnOpen()
    {
        _autoSaveOnSnapshot = EditorPrefs.GetBool(MemPrefs.AutoSaveOnSnapshot);
        _diffHideIdentical = EditorPrefs.GetBool(MemPrefs.Diff_HideIdentical);
        _diffHideRemoved = EditorPrefs.GetBool(MemPrefs.Diff_HideRemoved);
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(200, 80);
    }

    public override void OnGUI(Rect rect)
    {
        EditorGUILayout.BeginVertical();
        GUILayout.Space(5);
        ShowConfigBool(ref _autoSaveOnSnapshot, "Auto-Save on snapshot", MemPrefs.AutoSaveOnSnapshot);
        ShowConfigBool(ref _diffHideIdentical, "Diff - Hide Identical", MemPrefs.Diff_HideIdentical);
        ShowConfigBool(ref _diffHideRemoved, "Diff - Hide Removed", MemPrefs.Diff_HideRemoved);
        EditorGUILayout.EndVertical();
    }

    void ShowConfigBool(ref bool configVar, string text, string prefText)
    {
        bool newVal = EditorGUILayout.Toggle(text, configVar);
        if (configVar != newVal)
        {
            configVar = newVal;
            EditorPrefs.SetBool(prefText, configVar);
        }
    }
}
