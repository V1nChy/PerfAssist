using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

public delegate void SelectionHandler(object selected, int col);

public partial class TableView : IDisposable
{
    private int _shownLineCount = 0;

    // show internal sequential ID in the first column
    public bool ShowInternalSeqID = false;

    public event SelectionHandler OnSelected;

    public TableViewAppr Appearance { get { return _appearance; } }

    public TableView(EditorWindow hostWindow, Type itemType)
    {
        m_hostWindow = hostWindow;
        m_itemType = itemType;
    }

    public void Dispose()
    {

    }

    public void ClearColumns()
    {
        m_descArray.Clear();
    }

    public bool AddColumn(string colDataPropertyName, string colTitleText, float widthByPercent, TextAnchor alignment = TextAnchor.MiddleCenter, string fmt = "")
    {
        TableViewColDesc desc = new TableViewColDesc();
        desc.PropertyName = colDataPropertyName;
        desc.TitleText = colTitleText;
        desc.Alignment = alignment;
        desc.WidthInPercent = widthByPercent;
        desc.Format = string.IsNullOrEmpty(fmt) ? null : fmt;
        desc.MemInfo = m_itemType.GetField(desc.PropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
        if (desc.MemInfo == null)
        {
            desc.MemInfo = m_itemType.GetProperty(desc.PropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
            if (desc.MemInfo == null)
            {
                Debug.LogWarningFormat("Field '{0}' accessing failed.", desc.PropertyName);
                return false;
            }
        }

        m_descArray.Add(desc);
        return true;
    }

    public void RefreshData(List<object> entries, Dictionary<object, Color> specialTextColors = null)
    {
        m_lines.Clear();

        if (entries != null && entries.Count > 0)
        {
            m_lines.AddRange(entries);

            SortData();
        }

        m_specialTextColors = specialTextColors;
    }

    public void Draw(Rect area)
    {
        GUILayout.BeginArea(area);

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
        //Debug.LogFormat("scroll pos: {0:0.00}, {1:0.00}", _scrollPos.x, _scrollPos.y);
        {
            GUIStyle s = new GUIStyle();
            s.fixedHeight = _appearance.LineHeight * (m_lines.Count + 1);
            s.stretchWidth = true;
            Rect r = EditorGUILayout.BeginVertical(s);
            {
                // this silly line (empty label) is required by Unity to ensure the scroll bar appear as expected.
                PAEditorUtil.DrawLabel("", _appearance.Style_Line);

                DrawTitle(r.width);

                // these first/last calculatings are for smart clipping 
                int firstLine = Mathf.Max((int)(_scrollPos.y / _appearance.LineHeight) - 1, 0);
                _shownLineCount = (int)(area.height / _appearance.LineHeight) + 2;
                int lastLine = Mathf.Min(firstLine + _shownLineCount, m_lines.Count);

                for (int i = firstLine; i < lastLine; i++)
                {
                    DrawLine(i + 1, m_lines[i], r.width);
                }
            }
            EditorGUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    public void MoveSelectItem(int value)
    {
        m_selectedPos += value;
        m_selectedPos = Mathf.Clamp(m_selectedPos, 0, m_lines.Count - 1);
        m_selected = m_lines[m_selectedPos];

        int firstLine = Mathf.Max((int)(_scrollPos.y / _appearance.LineHeight) - 1, 0);
        int lastLine = Mathf.Min(firstLine + _shownLineCount, m_lines.Count);
        if (lastLine >= _shownLineCount && m_selectedPos > lastLine - 4)
        {
            _scrollPos.y = (m_selectedPos + 4 - _shownLineCount) * _appearance.LineHeight;
        }
        else if (lastLine >= _shownLineCount && m_selectedPos < firstLine + 1)
        {
            _scrollPos.y = m_selectedPos * _appearance.LineHeight;
        }

        if (OnSelected != null)
            OnSelected(m_selected, m_selectedCol);

        m_hostWindow.Repaint();
    }


    public void SetSortParams(int sortSlot, bool descending)
    {
        _sortSlot = sortSlot;
        _descending = descending;
    }

    public void SetSelected(object obj)
    {
        m_selected = obj;

        if(obj == null)
        {
            m_selectedPos = -1;
            m_selectedCol = -1;
        }
        else
        {
            for(int i = 0; i < m_lines.Count; i++)
            {
                if(obj == m_lines[i])
                {
                    m_selectedPos = i;
                    break;
                }
            }
        }


        if (OnSelected != null)
            OnSelected(obj, 0);
    }

    public object GetSelected()
    {
        return m_selected;
    }
}
