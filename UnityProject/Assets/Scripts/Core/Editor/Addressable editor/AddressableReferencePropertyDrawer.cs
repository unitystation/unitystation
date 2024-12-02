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
	private AudioSource previewAudioSource;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		float labelWidth = position.width * 0.6f;
		float buttonWidth = position.width * 0.3f;
		float previewButtonWidth = position.width * 0.1f;

		Rect labelPosition = new Rect(position.x, position.y, labelWidth, position.height);
		Rect buttonPosition = new Rect(position.x + labelWidth, position.y, buttonWidth, position.height);
		Rect previewButtonPosition = new Rect(position.x + labelWidth + buttonWidth, position.y, previewButtonWidth, position.height);

		// Properly handle indentation in lists
		labelPosition = EditorGUI.IndentedRect(labelPosition);

		var Path = property.FindPropertyRelative("AssetAddress");
		string stringPath = Path.stringValue;
		if (string.IsNullOrEmpty(stringPath))
		{
			stringPath = "Null";
		}

		// Draw Label
		EditorGUI.LabelField(labelPosition, $"{property.displayName}");

		// Draw Select Button
		if (GUI.Button(buttonPosition, $"{stringPath}", EditorStyles.popup))
		{
			SearchWindow.Open(
				new SearchWindowContext(GUIUtility.GUIToScreenPoint(UnityEngine.Event.current.mousePosition)),
				new StringSearchList(AddressablePicker.options["SoundAndMusic"], s =>
				{
					Path.stringValue = s;
					Path.serializedObject.ApplyModifiedProperties();
				}));
		}

		// Draw Preview Button
		if (GUI.Button(previewButtonPosition, "▶"))
		{
			PlayAudioPreview(stringPath);
		}

		EditorGUI.EndProperty();
	}


	/// <summary>
	/// Plays the audio preview in the editor.
	/// </summary>
	/// <param name="address">The addressable asset path.</param>
	private void PlayAudioPreview(string address)
	{
		if (string.IsNullOrEmpty(address) || address == "Null")
		{
			Debug.LogWarning("No valid addressable audio selected for preview.");
			return;
		}

		// Load the audio clip
		Addressables.LoadAssetAsync<GameObject>(address).Completed += handle =>
		{
			try
			{
				AudioSource clip = handle.Result.GetComponent<AudioSource>();
				foreach (var compo in handle.Result.GetComponents<Component>())
				{
					Debug.Log($"{compo.GetType()} - {compo.name}");
				}
				if (clip == null)
				{
					Debug.LogError($"AudioClip not found at address: {address}");
					return;
				}

				GameObject audioPreviewObject = new GameObject("SoundSpawn");
				previewAudioSource = audioPreviewObject.AddComponent<AudioSource>();
				previewAudioSource.clip = handle.Result.GetComponent<AudioSource>().clip;
				previewAudioSource.loop = false;
				previewAudioSource.spatialBlend = 0;
				previewAudioSource.spatialize = false;
				previewAudioSource.maxDistance = Single.MaxValue;
				if (previewAudioSource.isPlaying == false) previewAudioSource.Play();
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to load AudioClip at address: {address}.\n {e}");
			}
		};
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