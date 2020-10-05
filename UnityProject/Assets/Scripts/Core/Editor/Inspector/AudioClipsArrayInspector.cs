using Audio.Containers;
using UnityEditor;
using UnityEngine;


namespace CustomInspectors
{
	[CustomEditor(typeof(AudioClipsArray))]
	public class AudioClipsArrayInspector : Editor
	{
		private SerializedProperty audioClips;
		private AudioSource audioSource;
		private int currentPickerWindow;
		private bool isPickerWindowClosed = false;
		private Object valueToAdd;

		private void OnEnable()
		{
			audioClips = serializedObject.FindProperty("audioClips");
			audioSource = EditorUtility.CreateGameObjectWithHideFlags("Audio preview",HideFlags.HideAndDontSave,typeof(AudioSource)).GetComponent<AudioSource>();
		}

		private void OnDisable()
		{
			DestroyImmediate(audioSource.gameObject);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			for (int i = 0; i < audioClips.arraySize; i++)
			{

				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.LabelField(i.ToString(),GUILayout.MaxWidth(30), GUILayout.MaxHeight(20));

				var objectReference = audioClips.GetArrayElementAtIndex(i).objectReferenceValue;
				EditorGUILayout.ObjectField(objectReference, typeof(AudioClip),false,
								GUILayout.MaxHeight(20));

				if (GUILayout.Button("P", GUILayout.MaxWidth(20), GUILayout.MaxHeight(20)))
				{
					var audioClip = (AudioClip) objectReference;
					audioSource.clip = audioClip;

					audioSource.Play();
				}

				if (GUILayout.Button("S", GUILayout.MaxWidth(20), GUILayout.MaxHeight(20)))
				{
					audioSource.Stop();
				}

				if (GUILayout.Button("X", GUILayout.MaxWidth(20), GUILayout.MaxHeight(20)))
				{
					audioSource.Stop();
					audioClips.DeleteArrayElementAtIndex(i);
				}

				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add clip"))
			{
				//create a window picker control ID
				currentPickerWindow = EditorGUIUtility.GetControlID(FocusType.Passive);

				//use the ID you just created
				EditorGUIUtility.ShowObjectPicker<AudioClip>(null,false,"",currentPickerWindow);
				isPickerWindowClosed = false;
			}

			AddNewClipFromPickerWindow();

			serializedObject.ApplyModifiedProperties();
		}

		private void AddNewClipFromPickerWindow()
		{
			var currentPickedValue = EditorGUIUtility.GetObjectPickerObject();

			if (currentPickedValue != null)
			{
				valueToAdd = currentPickedValue;
			}

			if (Event.current.commandName == "ObjectSelectorClosed")
			{
				isPickerWindowClosed = true;
			}

			if (Event.current.type == EventType.Repaint && valueToAdd != null && isPickerWindowClosed)
			{
				audioClips.InsertArrayElementAtIndex(0);
				audioClips.GetArrayElementAtIndex(0).objectReferenceValue = valueToAdd;
				valueToAdd = null;
			}
		}
	}
}