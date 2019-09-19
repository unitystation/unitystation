using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpriteHandler))]
public class SpriteHandlerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		SpriteHandler myScript = (SpriteHandler) target;

		if (GUILayout.Button("SetUpSheet"))
		{
			myScript.SetUpSheet();
		}
	}
}