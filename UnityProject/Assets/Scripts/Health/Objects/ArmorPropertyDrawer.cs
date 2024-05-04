using UnityEditor;
using UnityEngine;

namespace Health.Objects
{
	[CustomPropertyDrawer(typeof(Armor))]
	public class ArmorDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PropertyField(position, property, label, true);
			float totalHeight = EditorGUI.GetPropertyHeight(property, label, true);
			if (property.isExpanded)
			{
				position.y += totalHeight / 2.1f;
				SerializedProperty temperatureProtectionInK = property.FindPropertyRelative("TemperatureProtectionInK");

				if (temperatureProtectionInK != null)
				{
					float celsiusX = temperatureProtectionInK.vector2Value.x - 273.15f;
					float celsiusY = temperatureProtectionInK.vector2Value.y - 273.15f;
					var cLabelText = "Temperature (°C): " + celsiusX.ToString("F2") + " - " + celsiusY.ToString("F2");
					EditorGUI.LabelField(position, cLabelText);
					position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

					float fahrenheitx = (temperatureProtectionInK.vector2Value.x - 273.15f) * 9 / 5 + 32;
					float fahrenheity = (temperatureProtectionInK.vector2Value.y - 273.15f) * 9 / 5 + 32;
					var fLabelText = "Temperature (°F): " + fahrenheitx.ToString("F2") + " - " + fahrenheity.ToString("F2");
					EditorGUI.LabelField(position, fLabelText);
				}
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUI.GetPropertyHeight(property, label, true);
			if (property.isExpanded)
			{
				height += EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 2; // Height for two additional labels
			}
			return height;
		}
	}
}