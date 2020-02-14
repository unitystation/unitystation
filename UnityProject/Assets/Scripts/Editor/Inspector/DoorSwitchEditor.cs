using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DoorSwitch))]
public class DoorSwitchEditor : Editor
{
	private DoorSwitch doorSwitch;
	private bool isSelecting;

	void OnEnable() { EditorApplication.update += Update; }
	void OnDisable() { EditorApplication.update -= Update; }

	void Update()
	{
		//skip if not setup yet
		if (!isSelecting || doorSwitch == null)
			return;

		//if a new object was selected
		GameObject newSelectedObject = (GameObject)Selection.activeObject;
		if (newSelectedObject != doorSwitch.gameObject)
		{
			//remove controller if found, add if not
			var doorController = newSelectedObject.GetComponent<DoorController>();
			if (doorController != null)
			{
				if (doorSwitch.doorControllers.Contains(doorController))
				{
					var list = doorSwitch.doorControllers.ToList();
					list.Remove(doorController);
					doorSwitch.doorControllers = list.ToArray();
				}
				else
				{
					var list = doorSwitch.doorControllers.ToList();
					list.Add(doorController);
					doorSwitch.doorControllers = list.ToArray();
				}
			}

			//return selection to switch
			Selection.activeGameObject = doorSwitch.gameObject;
		}
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (!isSelecting)
		{
			if (GUILayout.Button("Begin Selecting DoorControllers"))
			{
				isSelecting = true;
				doorSwitch = (DoorSwitch)target;
			}
		}
		else
		{
			if (GUILayout.Button("Stop Selecting DoorControllers"))
			{
				isSelecting = false;
				doorSwitch = null;
			}
		}
	}
}
