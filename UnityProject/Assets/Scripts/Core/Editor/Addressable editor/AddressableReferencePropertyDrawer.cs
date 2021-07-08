using AddressableReferences;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CustomPropertyDrawer(typeof(AddressableTexture))]
[CustomPropertyDrawer(typeof(AddressableSprite))]
[CustomPropertyDrawer(typeof(AddressableAudioSource))]
public class AddressableReferencePropertyDrawer : PropertyDrawer
{
	private string[] temarry;
	int _choiceIndex = 0;
	private string searchString = "";
	private const int Height = 24;
	private const int x = 40;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		float width = position.width - x;
		//EditorGUI.LabelField(new Rect(x, position.y * (Height * 0), width, Height), label); TODO: fix this screwing up GUI layout
		string labelText = label.text;
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		string AddressableType = "";

		if (property.name == "AddressableAudioSource")
		{
			AddressableType = "SoundAndMusic";
		}
		else
		{
			AddressableType = "SoundAndMusic";
		}

		bool Refresh = GUILayout.Button("Refresh catalogue");
		if (Refresh)
		{
			AddressablePicker.Refresh();
		}
		//EditorGUI.indentLevel++;
		EditorGUI.BeginChangeCheck();
		var Path = property.FindPropertyRelative("AssetAddress");
		var AssetReference = property.FindPropertyRelative("AssetReference");

		EditorGUI.PropertyField(new Rect(x, position.y + (Height * 1), width, Height),
			property.FindPropertyRelative("SetLoadSetting"), new GUIContent("Dispose Mode"));

		//EditorGUI.BeginDisabledGroup(true);
		EditorGUI.PropertyField(new Rect(x, position.y + (Height * 2), width, Height), Path,
			new GUIContent("Addressable Path"));
		//EditorGUI.EndDisabledGroup();

		//UnityEngine.Object oldAssetReference = AssetReference.objectReferenceValue;
		EditorGUI.indentLevel++;
		var newsearchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"));

		if (newsearchString != searchString)
		{
			//searchChange = true;
			searchString = newsearchString;
		}


		if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
		{
			// Remove focus if cleared
			searchString = "";
			GUI.FocusControl(null);
		}

		var inint = 0;
		if (searchString != "")
		{
			temarry = (AddressablePicker.options[AddressableType].Where(x =>
				x.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) >= 0)).ToArray();
			inint = temarry.ToList().IndexOf(Path.stringValue);
		}
		else
		{
			inint = AddressablePicker.options[AddressableType].ToList().IndexOf(Path.stringValue);
		}

		if (inint == -1)
		{
			inint = 0;
		}

		if (searchString != "")
		{
			_choiceIndex = EditorGUILayout.Popup(inint, temarry);
		}
		else
		{
			_choiceIndex = EditorGUILayout.Popup(inint, AddressablePicker.options[AddressableType]);
		}



		EditorGUI.indentLevel--;
		if (searchString != "")
		{
			if (temarry.Length <= _choiceIndex)
			{
				if (temarry.Length > 0)
				{
					Path.stringValue = temarry[0];
				}
				else
				{
					Path.stringValue = "None";
				}
			}
			else
			{
				Path.stringValue = temarry[_choiceIndex];
			}
		}
		else
		{
			Path.stringValue = AddressablePicker.options[AddressableType][_choiceIndex];
		}

		EditorGUI.PropertyField(new Rect(x, position.y + (Height * 3), width, Height), AssetReference,
			new GUIContent("AssetReference"));

		// Drag & Drop of AssetReference doesn't seem to trigger EndChangeCheck.  So, we verify it manually.
		if (EditorGUI.EndChangeCheck())
		{
			var m_AssetRefObject =
				SerializedPropertyExtensions.GetActualObjectForSerializedProperty<AssetReference>(AssetReference,
					fieldInfo, ref labelText);
			var Addressable = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(m_AssetRefObject.AssetGUID);
			if (Addressable != null)
			{
				Path.stringValue = AddressableAssetSettingsDefaultObject.Settings
					.FindAssetEntry(m_AssetRefObject.AssetGUID).address;
			}
		}



		//EditorGUI.indentLevel--;
		property.serializedObject.ApplyModifiedProperties();
		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return Height * 4;
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
		public static T GetActualObjectForSerializedProperty<T>(SerializedProperty property, FieldInfo field,
			ref string label)
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
