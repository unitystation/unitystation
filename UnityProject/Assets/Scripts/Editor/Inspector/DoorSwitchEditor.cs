using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(DoorSwitch))]
public class DoorSwitchEditor : Editor
{
	private DoorSwitch doorSwitch;
	private bool isSelecting;

	void OnEnable()
	{
		SceneView.duringSceneGui += OnScene;
	}

	void OnDisable()
	{
		SceneView.duringSceneGui -= OnScene;
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

	void OnScene(SceneView scene)
	{
		//skip if not selecting
		if (!isSelecting || doorSwitch == null)
			return;

		Event e = Event.current;
		if (e == null)
			return;

		if (HasPressedEscapeKey(e))
		{
			isSelecting = false;
			return;
		}

		if (HasPressedLeftClick(e))
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
			RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

			// scan all hit objects for door controllers
			for (int i = 0; i < hits.Length; i++)
			{
				var doorController = hits[i].transform.GetComponent<DoorController>();
				if (doorController != null)
					ToggleDoorController(doorSwitch, doorController);
			}

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}

		//return selection to switch
		Selection.activeGameObject = doorSwitch.gameObject;
	}

	private void ToggleDoorController(DoorSwitch doorSwitch, DoorController doorController)
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

	private bool HasPressedEscapeKey(Event e)
	{
		return e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape;
	}

	private bool HasPressedLeftClick(Event e)
	{
		return e.type == EventType.MouseDown && e.button == 0;
	}
}
