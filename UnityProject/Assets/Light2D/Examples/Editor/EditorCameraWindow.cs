using UnityEditor;
using UnityEngine;

public class EditorCameraWindow : EditorWindow
{
    private bool groupEnabled;
    private bool myBool = true;
    private float myFloat = 1.23f;
    private string myString = "Hello World";

    [MenuItem("Window/Editor Camera Settings")]
    private static void Init()
    {
        GetWindow<EditorCameraWindow>();
    }

    private void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        myString = EditorGUILayout.TextField("Text Field", myString);

        groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
        myBool = EditorGUILayout.Toggle("Toggle", myBool);
        myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
        EditorGUILayout.EndToggleGroup();
    }
}