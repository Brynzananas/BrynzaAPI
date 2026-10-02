using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BrynzaAPI;
[CustomEditor(typeof(BulletPatternDef))]
public class BulletPatternDefEditor : Editor
{
    private BulletPatternDef pattern;
    private int selectedPointIndex = -1;
    private const float PointRadius = 6f;
    private void OnEnable()
    {
        pattern = (BulletPatternDef)target;
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Spread Canvas (Left-Click to Add/Drag, Right-Click to Delete)", EditorStyles.boldLabel);
        Rect canvasRect = GUILayoutUtility.GetRect(200, 200, GUILayout.Width(500), GUILayout.Height(500));
        DrawCanvas(canvasRect);
        HandleCanvasEvents(canvasRect);
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Points"))
        {
            Undo.RecordObject(pattern, "Clear Bullet Pattern Points");
            pattern.points.Clear();
            EditorUtility.SetDirty(pattern);
        }
        if (GUILayout.Button("Add Center Point") && pattern.points.Count == 0)
        {
            Undo.RecordObject(pattern, "Add Center Point");
            pattern.points.Add(Vector2.zero);
            EditorUtility.SetDirty(pattern);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("points"), true);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCanvas(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
        Handles.color = Color.gray;
        Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Color.black);
        Vector2 center = rect.center;
        Handles.color = new Color(1f, 1f, 1f, 0.25f);
        Handles.DrawLine(new Vector3(rect.xMin, center.y), new Vector3(rect.xMax, center.y));
        Handles.DrawLine(new Vector3(center.x, rect.yMin), new Vector3(center.x, rect.yMax));
        if (pattern.points.Count > 1)
        {
            Handles.color = new Color(0.2f, 0.6f, 1f, 0.5f);
            for (int i = 0; i < pattern.points.Count - 1; i++)
            {
                Vector2 p1 = NormalizedToCanvas(pattern.points[i], rect);
                Vector2 p2 = NormalizedToCanvas(pattern.points[i + 1], rect);
                Handles.DrawLine(p1, p2);
            }
        }
        for (int i = 0; i < pattern.points.Count; i++)
        {
            Vector2 canvasPos = NormalizedToCanvas(pattern.points[i], rect);
            Handles.color = (i == selectedPointIndex) ? Color.yellow : Color.red;
            Handles.DrawSolidDisc(canvasPos, Vector3.forward, PointRadius);
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(canvasPos.x - 15, canvasPos.y + 4, 30, 20), (i + 1).ToString(), labelStyle);
        }
    }

    private void HandleCanvasEvents(Rect rect)
    {
        Event e = Event.current;
        if (!rect.Contains(e.mousePosition)) return;
        Vector2 mouseNormalized = CanvasToNormalized(e.mousePosition, rect);
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    int clickedIndex = GetPointAtPosition(e.mousePosition, rect);
                    if (clickedIndex != -1)
                    {
                        selectedPointIndex = clickedIndex;
                    }
                    else
                    {
                        Undo.RecordObject(pattern, "Add Spread Point");
                        pattern.points.Add(mouseNormalized);
                        selectedPointIndex = pattern.points.Count - 1;
                        EditorUtility.SetDirty(pattern);
                    }
                    e.Use();
                }
                else if (e.button == 1)
                {
                    int clickedIndex = GetPointAtPosition(e.mousePosition, rect);
                    if (clickedIndex != -1)
                    {
                        Undo.RecordObject(pattern, "Remove Spread Point");
                        pattern.points.RemoveAt(clickedIndex);
                        selectedPointIndex = -1;
                        EditorUtility.SetDirty(pattern);
                        e.Use();
                    }
                }
                break;
            case EventType.MouseDrag:
                if (e.button == 0 && selectedPointIndex >= 0 && selectedPointIndex < pattern.points.Count)
                {
                    Undo.RecordObject(pattern, "Move Spread Point");
                    pattern.points[selectedPointIndex] = mouseNormalized;
                    EditorUtility.SetDirty(pattern);
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                if (e.button == 0)
                {
                    selectedPointIndex = -1;
                }
                break;
        }
    }

    private Vector2 NormalizedToCanvas(Vector2 norm, Rect rect)
    {
        float x = Mathf.Lerp(rect.xMin, rect.xMax, (norm.x + 1f) * 0.5f);
        float y = Mathf.Lerp(rect.yMax, rect.yMin, (norm.y + 1f) * 0.5f);
        return new Vector2(x, y);
    }

    private Vector2 CanvasToNormalized(Vector2 canvasPos, Rect rect)
    {
        float x = Mathf.Clamp((canvasPos.x - rect.xMin) / rect.width * 2f - 1f, -1f, 1f);
        float y = Mathf.Clamp(1f - (canvasPos.y - rect.yMin) / rect.height * 2f, -1f, 1f);
        return new Vector2(x, y);
    }

    private int GetPointAtPosition(Vector2 mousePos, Rect rect)
    {
        for (int i = 0; i < pattern.points.Count; i++)
        {
            Vector2 pointCanvasPos = NormalizedToCanvas(pattern.points[i], rect);
            if (Vector2.Distance(pointCanvasPos, mousePos) <= PointRadius + 4f)
            {
                return i;
            }
        }
        return -1;
    }
}
