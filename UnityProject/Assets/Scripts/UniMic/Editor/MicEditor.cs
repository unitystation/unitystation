#if UNITY_EDITOR

using UnityEditor;

using UnityEngine;

namespace Adrenak.UniMic {
	[CustomEditor(typeof(Mic))]
	public class MicEditor : Editor {
		private bool showInfo;

		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			var mic = (Mic)target;

			// Present a dropdown menu listing the possible styles
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Select Device");
			if (EditorGUILayout.DropdownButton(new GUIContent(mic.CurrentDeviceName), FocusType.Keyboard)) {
				var menu = new GenericMenu();

				foreach (var device in mic.Devices)
					menu.AddItem(new GUIContent(device), mic.Devices[mic.CurrentDeviceIndex] == device,
						OnDeviceSelected, device);

				menu.ShowAsContext();
			}

			EditorGUILayout.EndHorizontal();

			// Display a button to reset to the system's default mic
			if (GUILayout.Button("Switch to System's Default Device"))
				mic.SetDeviceIndex(0);

			if (showInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showInfo, "Debug Info")) {
				GUI.enabled = false;

				EditorGUILayout.IntField("Device Index", mic.CurrentDeviceIndex);
				EditorGUILayout.LabelField("Device Name", mic.CurrentDeviceName);

				EditorGUILayout.Toggle("Is Recording", mic.IsRecording);
				EditorGUILayout.IntField("Frequency", mic.Frequency);
				EditorGUILayout.IntField("Sample Duration (ms)", mic.SampleDurationMS);

				GUI.enabled = true;
			}
		}

		// Handler for when a menu item is selected
		private void OnDeviceSelected(object device_) {
			var device = (string)device_;
			var mic = (Mic)target;

			mic.SetDeviceIndex(mic.Devices.FindIndex(x => x == device));
		}
	}
}
#endif