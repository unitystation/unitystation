/*using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpriteHandler))]
public class SpriteHandlerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		SpriteHandler electricalManager = (SpriteHandler)target;

		DrawDefaultInspector();

		EditorGUI.BeginChangeCheck();
		EditorGUI.PropertyField(r, sp, GUIContent.none);
		if (EditorGUI.EndChangeCheck())
		{
			// Do something when the property changes
		}
	}

	private static void AddButtonGroup(SpriteHandler spriteHandler)
	{
		EditorGUILayout.BeginHorizontal();



		EditorGUILayout.EndHorizontal();
	}

}
#endif*/