using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using System;
using UnityEngine.SceneManagement;


#if UNITY_EDITOR
using UnityEditor;


[CustomPropertyDrawer(typeof(SpriteSheetAndData))]
public class SpriteSheetAndDataPropertyDrawer : PropertyDrawer
{
	public static Type TupleTypeReference = Type.GetType("System.ITuple, mscorlib");
	// Draw the property inside the given rect
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty(position, label, property);

		//// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		// Calculate rects
		SerializedProperty texProp = property.FindPropertyRelative("Texture");
		float texPropHeight = EditorGUI.GetPropertyHeight(texProp);
		var amountRect = new Rect(position.x, position.y, position.width, texPropHeight);

		// //Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.BeginChangeCheck();
		EditorGUI.PropertyField(amountRect, texProp, GUIContent.none);
		property.serializedObject.ApplyModifiedProperties();
		if (EditorGUI.EndChangeCheck())
		{
			var BaseSerialiseObject = property.serializedObject.targetObject;
			GetAttributes(BaseSerialiseObject as object);
		}
		EditorGUI.PropertyField(new Rect(1000, 1000, 1, 1), property.FindPropertyRelative("Sprites"), GUIContent.none);
		EditorGUI.PropertyField(new Rect(1001, 1001, 1, 1), property.FindPropertyRelative("EquippedData"), GUIContent.none);

		property.serializedObject.ApplyModifiedProperties();
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		float totalHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Texture"));

		return totalHeight;
	}

	public void GetAttributes(object Script, int Depth = 0)
	{
		Depth++;
		//if (Depth <= 10)
		//{
		//Logger.Log("1");
		Type monoType = Script.GetType();
		foreach (FieldInfo Field in monoType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
		{
			//Logger.Log("2");
			if (Field.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
			{
				//Logger.Log("3 " + Field.FieldType);
				if (Field.FieldType == typeof(SpriteSheetAndData))
				{
					//Logger.Log("4");
					(Field.GetValue(Script) as SpriteSheetAndData).setSprites();
				}
				//Logger.Log("5");

				ReflectionSpriteSheetAndData(Field.FieldType, Script, Info: Field, Depth: Depth);
			}
		}
		if (TupleTypeReference == monoType) //Causes an error if this is not here and Tuples can not get Custom properties so it is I needed to get the properties
		{
			foreach (PropertyInfo Properties in monoType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
			{
				if (Properties.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
				{
					if (Properties.PropertyType == typeof(SpriteSheetAndData))
					{
						(Properties.GetValue(Script) as SpriteSheetAndData).setSprites();
					}
					ReflectionSpriteSheetAndData(Properties.PropertyType, Script, PInfo: Properties, Depth: Depth);
				}
			}
		}
		//}
	}
	public void ReflectionSpriteSheetAndData(Type VariableType, object Script, FieldInfo Info = null, PropertyInfo PInfo = null, int Depth = 0)
	{
		if (Info == null && PInfo == null)
		{
			if (VariableType == typeof(SpriteSheetAndData))
			{
				(Script as SpriteSheetAndData).setSprites();
			}

			foreach (FieldInfo method in VariableType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
			{
				if (method.FieldType == typeof(SpriteSheetAndData))
				{
					(method.GetValue(Script) as SpriteSheetAndData).setSprites();
				}

				if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
				{
					if (method.FieldType.IsGenericType)
					{
						IEnumerable list = method.GetValue(Script) as IEnumerable;
						if (list != null)
						{
							foreach (var c in list)
							{
								Type valueType = c.GetType();
								ReflectionSpriteSheetAndData(c.GetType(), c);

							}
						}
					}
					else if (VariableType.IsClass && VariableType != typeof(string))
					{
						if (method.GetValue(Script) != null)
						{
							//Logger.Log(method.ToString());
							GetAttributes(method.GetValue(Script), Depth);
						}
					}
				}
			}
		}
		else {
			if (Info == null)
			{
				if (PInfo.PropertyType == typeof(SpriteSheetAndData))
				{
					(PInfo.GetValue(Script) as SpriteSheetAndData).setSprites();
				}
			}
			else
			{
				if (Info.FieldType == typeof(SpriteSheetAndData))
				{
					(Info.GetValue(Script) as SpriteSheetAndData).setSprites();
				}
			}


			if (VariableType.IsGenericType)
			{
				IEnumerable list;
				if (Info == null)
				{
					list = PInfo.GetValue(Script) as IEnumerable;
				}
				else
				{
					list = Info.GetValue(Script) as IEnumerable;
				}
				if (list != null)
				{
					foreach (object c in list)
					{
						ReflectionSpriteSheetAndData(c.GetType(), c);
					}
				}
			}
			else if (VariableType.IsClass && VariableType != typeof(string))
			{
				if (Info == null)
				{
					if (PInfo.GetValue(Script) != null)
					{
						GetAttributes(PInfo.GetValue(Script), Depth);
					}
				}
				else
				{
					if (Info.GetValue(Script) != null)
					{
						GetAttributes(Info.GetValue(Script), Depth);
					}
				}
			}
		}
	}
}
#endif


