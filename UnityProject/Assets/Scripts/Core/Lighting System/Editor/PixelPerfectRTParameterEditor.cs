
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

	[CustomPropertyDrawer(typeof(PixelPerfectRTParameter))]
public class PixelPerfectRTParameterEditor : PropertyDrawer
{
	public override void OnGUI(Rect iPosition, SerializedProperty iProperty, GUIContent iLabel)
	{
		var _position = iPosition;

		EditorGUI.BeginProperty(iPosition, iLabel, iProperty);

		_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Keyboard), iLabel);

		// Calculate rect.
		var _unitsRect = new Rect(_position.x, _position.y, _position.width * 0.666f, _position.height);
		var _pixelPerUnitRect = new Rect(_position.x + _position.width * 0.666f, _position.y, _position.width * 0.333f, _position.height);
		var _prefixRect = new Rect(_position.x - 15 + _position.width * 0.666f, _position.y, _position.width * 0.333f, _position.height);

		EditorGUI.PropertyField(_unitsRect, iProperty.FindPropertyRelative("units"), GUIContent.none);
		EditorGUI.PrefixLabel(_prefixRect, new GUIContent("P"));
		EditorGUI.PropertyField(_pixelPerUnitRect, iProperty.FindPropertyRelative("pixelPerUnit"), GUIContent.none);

		// Validate.
		// Units.
		var _units = iProperty.FindPropertyRelative("units").vector2IntValue;

		if (_units.x < 1)
		{
			_units.x = 1;
		}

		if (_units.y < 1)
		{
			_units.y = 1;
		}

		iProperty.FindPropertyRelative("units").vector2IntValue = _units;

		// Pixel Per Unit.
		if (iProperty.FindPropertyRelative("pixelPerUnit").intValue < 1)
		{
			iProperty.FindPropertyRelative("pixelPerUnit").intValue = 1;
		}

		EditorGUI.EndProperty();
	}
}

#endif
