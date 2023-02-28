using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Editor.Attributes;
using HealthV2.Living.PolymorphicSystems;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SelectImplementationAttribute))]
public class HealthSystemBasePropertyDrawer : PropertyDrawer
{
    private Type[] _implementations;
    private int _implementationTypeIndex;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
	    EditorGUI.BeginProperty(position, label, property);

	    position.height = EditorGUIUtility.singleLineHeight;

	    var dropdownWidth = position.width;
	    var dropdownPosition = new Rect(position.x, position.y, dropdownWidth, position.height);

	    if (_implementations == null)
		    _implementations = GetImplementations((attribute as SelectImplementationAttribute).FieldType).Where(impl => !impl.IsSubclassOf(typeof(UnityEngine.Object))).ToArray();

	    // Find the index of the current implementation type in the _implementations array
	    var currentValue = property.managedReferenceValue;
	    var currentIndex = currentValue == null ? -1 : Array.IndexOf(_implementations, currentValue.GetType());

	    var forceUpdate = currentValue == null;

	    // Set the initial value of _implementationTypeIndex to the index of the current implementation type, or 0 if currentValue is null
	    _implementationTypeIndex = currentIndex >= 0 ? currentIndex : 0;

	    // Show the dropdown and create an instance of the selected implementation type
	    EditorGUI.BeginChangeCheck();
	    _implementationTypeIndex = EditorGUI.Popup(dropdownPosition, "", _implementationTypeIndex, _implementations.Select(impl => impl.FullName).ToArray());
	    if (EditorGUI.EndChangeCheck() || forceUpdate)
	    {
		    var newImplementation = Activator.CreateInstance(_implementations[_implementationTypeIndex]);
		    property.managedReferenceValue = newImplementation;
	    }

	    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
	    position.height = EditorGUI.GetPropertyHeight(property);

	    EditorGUI.PropertyField(position, property, true);

	    EditorGUI.EndProperty();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = EditorGUIUtility.singleLineHeight * 1; // Add one line for the dropdown and four for the labels
        height += EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.standardVerticalSpacing; // Add one line for the PropertyField
        return height;
    }

    public static Type[] GetImplementations(Type interfaceType)
    {
        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
        return types.Where(p => interfaceType.IsAssignableFrom(p) && !p.IsAbstract).ToArray();
    }
}