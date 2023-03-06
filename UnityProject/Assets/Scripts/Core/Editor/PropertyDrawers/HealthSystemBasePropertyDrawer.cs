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
	    // Begin a new property group with EditorGUI
	    EditorGUI.BeginProperty(position, label, property);

	    // Set the height of the current position to be the height of a single line of text
	    position.height = EditorGUIUtility.singleLineHeight;

	    // Set the width of the dropdown menu to be the same as the position width
	    var dropdownWidth = position.width;
	    // Create a new rectangle at the same position as the original rectangle, but with the width of the dropdown menu
	    var dropdownPosition = new Rect(position.x, position.y, dropdownWidth, position.height);

	    // If the list of implementation types has not been created yet, create it now
	    if (_implementations == null)
	        // Get all classes that implement the specified attribute field type, but exclude any subclasses of UnityEngine.Object
	        _implementations = GetImplementations((attribute as SelectImplementationAttribute).FieldType)
	            .Where(impl => !impl.IsSubclassOf(typeof(UnityEngine.Object)))
	            .ToArray();

	    // Find the index of the current implementation type in the _implementations array
	    var currentValue = property.managedReferenceValue;
	    var currentIndex = currentValue == null ? -1 : Array.IndexOf(_implementations, currentValue.GetType());

	    // Set forceUpdate to true if the current value is null, otherwise set it to false
	    var forceUpdate = currentValue == null;

	    // Set the initial value of _implementationTypeIndex to the index of the current implementation type, or 0 if currentValue is null
	    _implementationTypeIndex = currentIndex >= 0 ? currentIndex : 0;

	    // Show the dropdown and create an instance of the selected implementation type
	    EditorGUI.BeginChangeCheck();
	    // Display a dropdown menu of all available implementation types, using their full names as the display text
	    _implementationTypeIndex = EditorGUI.Popup(dropdownPosition, "", _implementationTypeIndex, _implementations.Select(impl => impl.FullName).ToArray());
	    // If the dropdown selection has changed or the value was null, create a new instance of the selected implementation type
	    if (EditorGUI.EndChangeCheck() || forceUpdate)
	    {
	        var newImplementation = Activator.CreateInstance(_implementations[_implementationTypeIndex]);
	        property.managedReferenceValue = newImplementation;
	    }

	    // Move the position down by one line and set the height to be the height of the property field
	    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
	    position.height = EditorGUI.GetPropertyHeight(property);

	    // Display the property field for the current implementation
	    EditorGUI.PropertyField(position, property, true);

	    // End the current property group with EditorGUI
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