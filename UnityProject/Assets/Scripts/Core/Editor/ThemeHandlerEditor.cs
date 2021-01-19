using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unitystation.Options;

/// <summary>
/// Handles the Inspector for ThemeHandler
/// </summary>
[CustomEditor(typeof(ThemeHandler))]
public class ThemeHandlerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var handler = (ThemeHandler) target;
		EditorGUI.BeginChangeCheck();

		SerializedProperty ttype = serializedObject.FindProperty("themeType");
		EditorGUILayout.PropertyField(ttype, true);

		//Changes the Inspector fields depending on the UIElement selection:
		handler.targetElement = (UIElement) EditorGUILayout.EnumPopup("UIElement Type", handler.targetElement);
		if (handler.targetElement == UIElement.Text)
		{
			SerializedProperty t = serializedObject.FindProperty("text");
			if (handler.text == null)
			{
				handler.text = handler.GetComponent<Text>();
			}
			EditorGUILayout.PropertyField(t, true);
		}

		if (handler.targetElement == UIElement.Image)
		{
			SerializedProperty i = serializedObject.FindProperty("image");
			if (handler.image == null)
			{
				handler.image = handler.GetComponent<Image>();
			}
			EditorGUILayout.PropertyField(i, true);
		}

		if (handler.targetElement == UIElement.TextMeshProUGUI)
		{
			SerializedProperty tmpui = serializedObject.FindProperty("textMeshProUGUI");
			if (handler.textMeshProUGUI == null)
			{
				handler.textMeshProUGUI = handler.GetComponent<TMPro.TextMeshProUGUI>();
			}
			EditorGUILayout.PropertyField(tmpui, true);
		}

		//Add your custom views here

		if (EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
		}
	}
}
