using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MapPreview))]
public class MapPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var mapPreview = (MapPreview)target;
        if (GUILayout.Button("Generate")) mapPreview.DrawMapInEditor();
    }
}