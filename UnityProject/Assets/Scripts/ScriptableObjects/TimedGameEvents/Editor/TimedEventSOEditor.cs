#if UNITY_EDITOR
using ScriptableObjects.TimedGameEvents.SeasonalEvents;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ScriptableObjects.TimedGameEvents.Editor
{
	[CustomEditor(typeof(TimedGameEventSO), editorForChildClasses: true)]
	public class TimedGameEventSOEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			// Draw default fields for event name, description, etc.
			DrawDefaultInspector();

			// Create the button to open the custom calendar window
			if (GUILayout.Button("Open Calendar to Select Event Dates"))
			{
				// Open the custom window and pass the target event
				TimedGameEventSOCalendarWindow.ShowWindow((TimedGameEventSO)target);
			}
		}
	}
}
#endif