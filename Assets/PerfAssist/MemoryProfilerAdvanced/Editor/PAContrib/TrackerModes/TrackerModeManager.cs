using MemoryProfilerWindow;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;

public enum TrackerMode
{
    Editor,
    Remote,
    File,
}

public class TrackerModeManager : TrackerModeOwner
{
    private TrackerMode _currentMode = TrackerMode.Editor;
    public TrackerMode CurrentMode { get { return _currentMode; } }

    Dictionary<TrackerMode, TrackerMode_Base> _modes = new Dictionary<TrackerMode, TrackerMode_Base>()
    {
        { TrackerMode.Editor, new TrackerMode_Editor() },
        { TrackerMode.Remote, new TrackerMode_Remote() },
        { TrackerMode.File, new TrackerMode_File() },
    };

    public CrawledMemorySnapshot Selected { get { var curMode = GetCurrentMode(); return curMode != null ? curMode.Selected : null; } }
    public CrawledMemorySnapshot Diff_1st { get { var curMode = GetCurrentMode(); return curMode != null ? curMode.Diff_1st : null; } }
    public CrawledMemorySnapshot Diff_2nd { get { var curMode = GetCurrentMode(); return curMode != null ? curMode.Diff_2nd : null; } }

    private MemConfigPopup _configWindow = new MemConfigPopup();
    public bool AutoSaveOnSnapshot { get { return _configWindow.AutoSaveOnSnapshot; } }

    private Rect _optionPopupRect;

    public TrackerModeManager()
    {
        foreach (var t in _modes.Values)
            t.SetOwner(this);
    }

    public void Update()
    {
        var curMode = GetCurrentMode();
        if (curMode != null)
            curMode.Update();
    }

    public void Clear()
    {
        foreach (var t in _modes.Values)
            t.Clear();
    }

    public void OnGUI()
    {
        try
        {
            // shared functionalities
            GUILayout.BeginHorizontal(MemStyles.Toolbar);
            int gridWidth = 250;
            int selMode = GUI.SelectionGrid(new Rect(0, 0, gridWidth, 20), (int)_currentMode,
                TrackerModeConsts.Modes, TrackerModeConsts.Modes.Length, MemStyles.ToolbarButton);
            if (selMode != (int)_currentMode)
            {
                SwitchTo((TrackerMode)selMode);
            }
            GUILayout.Space(gridWidth + 10);
            GUIStyle style = new GUIStyle("CN StatusWarn");
            style.contentOffset = new Vector2(0,3);
            style.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(TrackerModeConsts.ModesDesc[selMode], style);
            GUILayout.FlexibleSpace();

            if (_currentMode == TrackerMode.File)
            {
                if (GUILayout.Button("Load File", MemStyles.ToolbarButton, GUILayout.MaxWidth(100)))
                {
                    TrackerMode_File mode = GetCurrentMode() as TrackerMode_File;
                    if (mode != null)
                    {
                        mode.LoadFile();
                        ChangeSnapshotSelection();
                    }
                }
                if (GUILayout.Button("Load Session", MemStyles.ToolbarButton, GUILayout.MaxWidth(100)))
                {
                    TrackerMode_File mode = GetCurrentMode() as TrackerMode_File;
                    if (mode != null)
                    {
                        mode.LoadSession();
                        ChangeSnapshotSelection();
                    }
                }
            }

            if (GUILayout.Button("Clear Session", MemStyles.ToolbarButton, GUILayout.MaxWidth(100)))
            {
                TrackerMode_Base mode = GetCurrentMode();
                if (mode != null)
                {
                    mode.Clear();
                }
            }
            if (GUILayout.Button("Open Dir", MemStyles.ToolbarButton, GUILayout.MaxWidth(100)))
            {
                EditorUtility.RevealInFinder(MemUtil.SnapshotsDir);
            }
            if (GUILayout.Button("Options", EditorStyles.toolbarDropDown, GUILayout.Width(100)))
            {
                try
                {
                    PopupWindow.Show(_optionPopupRect, _configWindow);
                }
                catch (ExitGUIException)
                {
                    // have no idea why Unity throws ExitGUIException() in GUIUtility.ExitGUI()
                    // so we silently ignore the exception 
                }
            }
            if (Event.current.type == EventType.Repaint)
                _optionPopupRect = GUILayoutUtility.GetLastRect();
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            // mode-specific controls
            GUILayout.BeginHorizontal();
            var curMode = GetCurrentMode();
            if (curMode != null)
                curMode.OnGUI();
            GUILayout.EndHorizontal();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public void AddSnapshot(PackedMemorySnapshot packed)
    {
        try
        {
            TrackerMode_Base curMode = GetCurrentMode();
            if (curMode == null)
            {
                Debug.LogErrorFormat("AddSnapshot() failed. (invalid mode: {0})", curMode);
                return;
            }

            var snapshotInfo = new MemSnapshotInfo();
            if (!snapshotInfo.AcceptSnapshot(packed))
            {
                Debug.LogError("AcceptSnapshot() failed.");
                return;
            }

            curMode.AddSnapshot(snapshotInfo);

            if (AutoSaveOnSnapshot)
            {
                if (!curMode.SaveSessionInfo(packed))
                    Debug.LogErrorFormat("Save Session Info Failed!");

                //if (!curMode.SaveSessionJson(snapshotInfo.Unpacked))
                //    Debug.LogErrorFormat("Save Session Json Failed!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public TrackerMode_Base GetCurrentMode()
    {
        TrackerMode_Base mode;
        if (!_modes.TryGetValue(_currentMode, out mode))
            return null;
        return mode;
    }

    public void SwitchTo(TrackerMode newMode)
    {
        if (_currentMode == newMode)
            return;

        TrackerMode_Base mode = GetCurrentMode();
        if (mode != null)
            mode.OnLeave();

        _currentMode = newMode;
        mode = GetCurrentMode();
        if (mode != null)
            mode.OnEnter();
    }
}
