using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SpriteHandler))]
public class SpriteHandlerInspector : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		SpriteHandler myScript = (SpriteHandler)target;
		if (GUILayout.Button("SetUpSheet"))
		{
			myScript.SetUpSheet();
		}
	}
}
#endif