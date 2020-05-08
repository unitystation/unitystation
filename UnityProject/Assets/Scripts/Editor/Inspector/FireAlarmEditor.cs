using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(FireAlarm),true )]
public class FireAlarmEditor : Editor
{
	private FireAlarm fireAlarm;
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
			if (GUILayout.Button("Begin Selecting Firelocks"))
			{
				isSelecting = true;
				fireAlarm = (FireAlarm)target;
			}
		}
		else
		{
			if (GUILayout.Button("Stop Selecting Firelocks"))
			{
				isSelecting = false;
				fireAlarm = null;
			}
		}
	}

	private void OnScene(SceneView scene)
	{
		if (!isSelecting || fireAlarm == null)
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

			for (int i = 0; i < hits.Length; i++)
			{
				var objectTrigger = hits[i].transform.GetComponent<FireLock>();
				if (objectTrigger != null)
					ToggleObjectTrigger(fireAlarm, objectTrigger);
			}

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}

		Selection.activeGameObject = fireAlarm.gameObject;
	}

	private void ToggleObjectTrigger(FireAlarm listOfTriggers, FireLock objectTrigger)
	{
		if (listOfTriggers.FireLockList.Contains(objectTrigger))
		{
			listOfTriggers.FireLockList.Remove(objectTrigger);
			objectTrigger.fireAlarm = null;

			EditorUtility.SetDirty(listOfTriggers);
			EditorUtility.SetDirty(objectTrigger);
		}
		else
		{
			listOfTriggers.FireLockList.Add(objectTrigger);
			objectTrigger.fireAlarm = listOfTriggers;

			EditorUtility.SetDirty(listOfTriggers);
			EditorUtility.SetDirty(objectTrigger);
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