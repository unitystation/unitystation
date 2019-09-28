using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpriteDataHandler))]
public class SpriteDataHandlerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		SpriteDataHandler myScript = (SpriteDataHandler)target;

		if (GUILayout.Button("SetUpSheet"))
		{
			myScript.SetUpSheet();
		}
	}
}