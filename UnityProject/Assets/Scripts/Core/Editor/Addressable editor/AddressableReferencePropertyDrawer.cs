using AddressableReferences;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CustomPropertyDrawer(typeof(AddressableTexture))]
[CustomPropertyDrawer(typeof(AddressableSprite))]
[CustomPropertyDrawer(typeof(AddressableAudioSource))]
public class AddressableReferencePropertyDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		string addressableType = "SoundAndMusic";
		GUILayout.BeginHorizontal();
		var Path = property.FindPropertyRelative("AssetAddress");
		string stringpath = Path.stringValue;
		if (string.IsNullOrEmpty(stringpath))
		{
			stringpath = "Null";
		}


		EditorGUILayout.LabelField($"{property.displayName}", GUILayout.ExpandWidth(false), GUILayout.Width(250));
		if (GUILayout.Button($"{stringpath}", EditorStyles.popup))
		{
			SearchWindow.Open(
				new SearchWindowContext(GUIUtility.GUIToScreenPoint((UnityEngine.Event.current.mousePosition))),
				new StringSearchList(AddressablePicker.options[addressableType], s =>
				{
					Path.stringValue = s;
					Path.serializedObject.ApplyModifiedProperties();
				}));

		}

		GUILayout.EndHorizontal();
		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return 0;
	}

	/// <summary>
	/// Used to manipulate data from a serialized property.
	/// </summary>
	public static class SerializedPropertyExtensions
	{
		/// <summary>
		/// Used to extract the target object from a serialized property.
		/// </summary>
		/// <typeparam name="T">The type of the object to extract.</typeparam>
		/// <param name="property">The property containing the object.</param>
		/// <param name="field">The field data.</param>
		/// <param name="label">The label name.</param>
		/// <returns>Returns the target object type.</returns>
		public static T GetActualObjectForSerializedProperty<T>(SerializedProperty property, FieldInfo field, ref string label)
		{
			try
			{
				if (property == null || field == null)
					return default(T);
				var serializedObject = property.serializedObject;
				if (serializedObject == null)
				{
					return default(T);
				}

				var targetObject = serializedObject.targetObject;

				if (property.depth > 0)
				{
					var slicedName = property.propertyPath.Split('.').ToList();
					List<int> arrayCounts = new List<int>();
					for (int index = 0; index < slicedName.Count; index++)
					{
						arrayCounts.Add(-1);
						var currName = slicedName[index];
						if (currName.EndsWith("]"))
						{
							var arraySlice = currName.Split('[', ']');
							if (arraySlice.Length >= 2)
							{
								arrayCounts[index - 2] = Convert.ToInt32(arraySlice[1]);
								slicedName[index] = string.Empty;
								slicedName[index - 1] = string.Empty;
							}
						}
					}

					while (string.IsNullOrEmpty(slicedName.Last()))
					{
						int i = slicedName.Count - 1;
						slicedName.RemoveAt(i);
						arrayCounts.RemoveAt(i);
					}

					if (property.propertyPath.EndsWith("]"))
					{
						var slice = property.propertyPath.Split('[', ']');
						if (slice.Length >= 2)
							label = "Element " + slice[slice.Length - 2];
					}
					else
					{
						label = slicedName.Last();
					}

					return DescendHierarchy<T>(targetObject, slicedName, arrayCounts, 0);
				}

				var obj = field.GetValue(targetObject);
				return (T) obj;
			}
			catch
			{
				return default(T);
			}
		}

		static T DescendHierarchy<T>(object targetObject, List<string> splitName, List<int> splitCounts, int depth)
		{
			if (depth >= splitName.Count)
				return default(T);

			var currName = splitName[depth];

			if (string.IsNullOrEmpty(currName))
				return DescendHierarchy<T>(targetObject, splitName, splitCounts, depth + 1);

			int arrayIndex = splitCounts[depth];

			var newField = targetObject.GetType().GetField(currName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			if (newField == null)
			{
				Type baseType = targetObject.GetType().BaseType;
				while (baseType != null && newField == null)
				{
					newField = baseType.GetField(currName,
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					baseType = baseType.BaseType;
				}
			}

			var newObj = newField.GetValue(targetObject);
			if (depth == splitName.Count - 1)
			{
				T actualObject = default(T);
				if (arrayIndex >= 0)
				{
					if (newObj.GetType().IsArray && ((System.Array) newObj).Length > arrayIndex)
						actualObject = (T) ((System.Array) newObj).GetValue(arrayIndex);

					var newObjList = newObj as IList;
					if (newObjList != null && newObjList.Count > arrayIndex)
					{
						actualObject = (T) newObjList[arrayIndex];

						//if (actualObject == null)
						//    actualObject = new T();
					}
				}
				else
				{
					actualObject = (T) newObj;
				}

				return actualObject;
			}
			else if (arrayIndex >= 0)
			{
				if (newObj is IList list)
				{
					newObj = list[arrayIndex];
				}
				else if (newObj is System.Array a)
				{
					newObj = a.GetValue(arrayIndex);
				}
			}

			return DescendHierarchy<T>(newObj, splitName, splitCounts, depth + 1);
		}
	}
}