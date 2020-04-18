using System.Linq;
using Mirror;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Objects;

[CustomEditor(typeof(SwitchBase),true )]
public class ListOfObjectsEditor : Editor
{
	private SwitchBase switchBase;
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
			if (GUILayout.Button("Begin Selecting Objects"))
			{
				isSelecting = true;
				switchBase = (SwitchBase)target;
			}
		}
		else
		{
			if (GUILayout.Button("Stop Selecting Objects"))
			{
				isSelecting = false;
				switchBase = null;
			}
		}
	}

	void OnScene(SceneView scene)
	{
		//skip if not selecting
		if (!isSelecting || switchBase == null)
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
				var objectTrigger = hits[i].transform.GetComponent<ObjectTrigger>();
				if (objectTrigger != null)
					ToggleObjectTrigger(switchBase, objectTrigger);
			}

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}

		//return selection to switch
		Selection.activeGameObject = switchBase.gameObject;
	}

	private void ToggleObjectTrigger(SwitchBase listOfTriggers, ObjectTrigger objectTrigger)
	{
		if (listOfTriggers.listOfTriggers.Contains(objectTrigger))
		{
			listOfTriggers.listOfTriggers.Remove(objectTrigger);
			EditorUtility.SetDirty(listOfTriggers);
		}
		else
		{
			listOfTriggers.listOfTriggers.Add(objectTrigger);
			EditorUtility.SetDirty(listOfTriggers);
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

