using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Allows using a property of the array element to display its name in the array rather than just
/// "Element X"
/// </summary>
[CustomPropertyDrawer(typeof(ArrayElementTitleAttribute))]
public class ArrayElementTitleDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
                                    GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
    protected virtual ArrayElementTitleAttribute Atribute
    {
        get { return (ArrayElementTitleAttribute)attribute; }
    }
    SerializedProperty TitleNameProp;
    public override void OnGUI(Rect position,
                              SerializedProperty property,
                              GUIContent label)
    {
        string FullPathName = property.propertyPath + "." + Atribute.Varname;
        TitleNameProp = property.serializedObject.FindProperty(FullPathName);
        string newlabel = GetTitle();
        if (string.IsNullOrEmpty(newlabel))
            newlabel = label.text;
        EditorGUI.PropertyField(position, property, new GUIContent(newlabel, label.tooltip), true);
    }
    private string GetTitle()
    {
        switch (TitleNameProp.propertyType)
        {
            case SerializedPropertyType.Generic:
                break;
            case SerializedPropertyType.Integer:
                return TitleNameProp.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return TitleNameProp.boolValue.ToString();
            case SerializedPropertyType.Float:
                return TitleNameProp.floatValue.ToString();
            case SerializedPropertyType.String:
                return TitleNameProp.stringValue;
            case SerializedPropertyType.Color:
                return TitleNameProp.colorValue.ToString();
            case SerializedPropertyType.ObjectReference:
                return TitleNameProp.objectReferenceValue == null ? Atribute.Nullname : TitleNameProp.objectReferenceValue.ToString();
            case SerializedPropertyType.LayerMask:
                break;
            case SerializedPropertyType.Enum:
	            return TitleNameProp.enumNames[TitleNameProp.enumValueIndex];
            case SerializedPropertyType.Vector2:
                return TitleNameProp.vector2Value.ToString();
            case SerializedPropertyType.Vector3:
                return TitleNameProp.vector3Value.ToString();
            case SerializedPropertyType.Vector4:
                return TitleNameProp.vector4Value.ToString();
            case SerializedPropertyType.Rect:
                break;
            case SerializedPropertyType.ArraySize:
                break;
            case SerializedPropertyType.Character:
                break;
            case SerializedPropertyType.AnimationCurve:
                break;
            case SerializedPropertyType.Bounds:
                break;
            case SerializedPropertyType.Gradient:
                break;
            case SerializedPropertyType.Quaternion:
                break;
            default:
                break;
        }
        return "";
    }
}
#endif

public class ArrayElementTitleAttribute : PropertyAttribute
{
	public string Varname;
	public string Nullname;
	public ArrayElementTitleAttribute(string ElementTitleVar, string NullvalueString = "null")
	{
		Varname = ElementTitleVar;
		Nullname = NullvalueString;
	}
}
