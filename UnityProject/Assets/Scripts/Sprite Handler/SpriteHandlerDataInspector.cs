using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SpriteHandlerData))]
public class SpriteHandlerDataInspector : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		SpriteHandlerData myScript = (SpriteHandlerData)target;
		if (GUILayout.Button("SetUpSheet"))
		{
			myScript.SetUpSheet();
		}
	}
}
#endif