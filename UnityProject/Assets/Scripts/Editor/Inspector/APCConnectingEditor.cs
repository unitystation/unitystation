using System.Linq;
using Mirror;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Objects;

[CustomEditor(typeof(APC),true )]
public class APCConnectingEditor : Editor
{
	private APC switchBase;
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
				switchBase = (APC)target;
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

	private void OnScene(SceneView scene)
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
				var objectTrigger = hits[i].transform.GetComponent<APCPoweredDevice>();
				if (objectTrigger != null)
					ToggleObjectTrigger(switchBase, objectTrigger);
			}

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}

		//return selection to switch
		Selection.activeGameObject = switchBase.gameObject;
	}

	private void ToggleObjectTrigger(APC listOfTriggers, APCPoweredDevice objectTrigger)
	{
		if (listOfTriggers.ConnectedDevices.Contains(objectTrigger))
		{
			listOfTriggers.ConnectedDevices.Remove(objectTrigger);
			objectTrigger.RelatedAPC = null;

			EditorUtility.SetDirty(listOfTriggers);
			EditorUtility.SetDirty(objectTrigger);
		}
		else
		{
			listOfTriggers.ConnectedDevices.Add(objectTrigger);
			objectTrigger.RelatedAPC = listOfTriggers;

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

