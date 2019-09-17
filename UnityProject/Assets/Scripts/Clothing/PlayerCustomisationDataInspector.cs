using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ResourceTracker))]
public class ResourceTrackerInspector : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		ResourceTracker myScript = (ResourceTracker)target;
		if (GUILayout.Button("SetUpSheet"))
		{
			myScript.GatherData();
		}
	}
}
#endif