using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(TemperatureAttribute))]
public class TemperatureAttributeEditor : PropertyDrawer
{
    private const float SelectorSize = 50f;
    private TemeratureUnits selectedUnit = TemeratureUnits.C;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var type = property.propertyType;

		if (type != SerializedPropertyType.Float)
		{
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        var enumRect = new Rect(position.x + position.width - SelectorSize, position.y,
			SelectorSize, position.height);
        selectedUnit = (TemeratureUnits)EditorGUI.EnumPopup(enumRect, selectedUnit);

        var tempK = property.floatValue;

		if (tempK < 0)
		{
			// you can't get lower than absolute zero
            tempK = 0f;
        }

        var temp = TemperatureUtils.FromKelvin(tempK, selectedUnit);

        var propRect = new Rect(position.x, position.y,
			position.width - SelectorSize, position.height);

        var newTemp = EditorGUI.FloatField(propRect, label, temp);
		var newTempK = TemperatureUtils.ToKelvin(newTemp, selectedUnit);

        if (Mathf.Abs(newTempK - tempK) > 0.001f)
        {
			// temperature has changed - update it
            property.floatValue = TemperatureUtils.ToKelvin(newTemp, selectedUnit);
        }
    }
}
