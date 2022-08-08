using System;
using Systems.Clearance;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Core.Editor
{
	[CustomPropertyDrawer(typeof(Clearance))]
	public class ClearancePropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			Rect labelposition = position;
			Rect buttonposition = position;
			labelposition.xMax -= 150;
			buttonposition.xMin += 150;

			EditorGUI.LabelField(labelposition,$"{property.displayName}");

			var nameInt = property.intValue;
			var name = "NULL";
			if (nameInt != 0)
			{
				name = ((Clearance)nameInt).ToString();
			}

			// EditorGUILayout.LabelField($"{property.displayName}", GUILayout.ExpandWidth(false), ;
			if (GUI.Button( buttonposition, $"{name}", EditorStyles.popup))
			{
				SearchWindow.Open(
					new SearchWindowContext(GUIUtility.GUIToScreenPoint((UnityEngine.Event.current.mousePosition))),
					new StringSearchList(Enum.GetNames(typeof(Clearance)), s =>
					{
						Enum.TryParse(s, out Clearance newEnum);
						property.intValue = (int) newEnum;
						property.serializedObject.ApplyModifiedProperties();
					}));

			}

			EditorGUI.EndProperty();
		}
	}
}