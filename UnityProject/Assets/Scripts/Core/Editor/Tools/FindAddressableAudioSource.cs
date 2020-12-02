using AddressableReferences;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor.Tools
{
	public class ComponentField
	{
		public GameObject GameObject;
		public Component Component;
		public string Field;
	}

	/// <summary>
	/// Tool that allows to find all game object that has a component with fields of a specific type.
	/// </summary>
	public class FindAddressableAudioSource : EditorWindow
	{
		private string propertyTypeToFind = "AddressableReferences.AddressableAudioSource,Assets";
		private List<ComponentField> objectsWithSearchedType = new List<ComponentField>();
		private bool nullOnly = false;
		private Vector2 scrollPosition;

		[MenuItem("Tools/Find AddressableAudioSource")]
		private static void Init()
		{
			// Get existing open window or if none, make a new one:
			FindAddressableAudioSource window = (FindAddressableAudioSource)GetWindow(typeof(FindAddressableAudioSource));
			window.titleContent.text = "Find AddressableAudioSource";
			window.Show();
		}

		private void Find()
		{
			string[] assetsGuids = AssetDatabase.FindAssets("t:GameObject");
			System.Type typeToFind = System.Type.GetType(propertyTypeToFind);

			objectsWithSearchedType.Clear();
			int counter = 0;
			foreach(string guid in assetsGuids)
			{
				counter++;
				EditorUtility.DisplayProgressBar("Searching...", $"{AssetDatabase.GUIDToAssetPath(guid)}", (float)counter / (float)(assetsGuids.Length - 1));

				Object obj = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
				Component[] components = ((GameObject)obj).GetComponentsInChildren<Component>(true);

				foreach (Component component in components)
				{
					FieldInfo[] fields = component.GetType().GetFields(BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

					List<FieldInfo> fieldInfos = fields.Where(p => p.FieldType == typeToFind).ToList();

					if (nullOnly && fieldInfos.Count > 0)
					{
						List<FieldInfo> notNullFieldInfos = new List<FieldInfo>();
						foreach(FieldInfo fieldInfo in fieldInfos)
						{
							AddressableAudioSource addressableAudioSource = (AddressableAudioSource)fieldInfo.GetValue(component);

							if ((addressableAudioSource.AssetReference == null) || (string.IsNullOrEmpty(addressableAudioSource.AssetReference.AssetGUID)))
								notNullFieldInfos.Add(fieldInfo);
						}

						fieldInfos = notNullFieldInfos;
					}

					foreach (FieldInfo fieldInfo in fieldInfos)
					{
						ComponentField componentField = new ComponentField();
						componentField.GameObject = (GameObject)obj;
						componentField.Component = component;
						componentField.Field = fieldInfo.Name;
						objectsWithSearchedType.Add(componentField);
					}
				}
			}
		}

		void OnGUI()
		{
			EditorGUILayout.HelpBox("Choose the name of the type you want to find\n\nYou can use the syntax described in .NET Type.GetType(string) documentation\n\nEx.: AddressableReferences.AddressableAudioSource,Assets for the type AddressableAudioSource, in namespace AddressableReferences in assembly Assets.", MessageType.Info);
			propertyTypeToFind = EditorGUILayout.TextField(propertyTypeToFind);

			EditorGUILayout.Separator();

			nullOnly = EditorGUILayout.Toggle("Search for null values only", nullOnly);
			
			EditorGUILayout.Separator();

			if (GUILayout.Button("Find"))
			{
				bool isOk = true;
				if (propertyTypeToFind == null)
				{
					EditorUtility.DisplayDialog("Error", "You must choose a type name to find", "OK");
					isOk = false;
				}

				System.Type type  = System.Type.GetType(propertyTypeToFind);
				if (type == null)
				{
					EditorUtility.DisplayDialog("Error", "Unknown type, try fully qualifying it with it's namespace and assembly (with a comma after the type name).", "OK");
					isOk = false;
				}

				if (isOk)
				{
					Find();
					EditorUtility.DisplayDialog("Complete", "It's done!", "OK");
				}
			}

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			foreach (ComponentField componentField in objectsWithSearchedType)
			{
				GUIContent guiContent = new GUIContent($"{componentField.GameObject.name} ({componentField.Component.name}) - {componentField.Field}");
				if (EditorGUILayout.DropdownButton(guiContent, FocusType.Passive))
				{
					Selection.activeObject = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(componentField.GameObject));
				}
			}

			EditorGUILayout.EndScrollView();
		}
	}
}
