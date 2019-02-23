using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;




/// <summary>
/// Custom Editor for the InputFieldFocus extended class which
/// lets you see and set the escape character
/// </summary>
[CustomEditor(typeof(InputFieldFocus))]
public class InputFieldFocusEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
	}
}
