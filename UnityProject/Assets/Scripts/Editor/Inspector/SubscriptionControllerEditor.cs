using System.Collections.Generic;
using System.Linq;
using Electric.Inheritance;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(SubscriptionController), true)]
public class SubscriptionControllerEditor : Editor
{
	private SubscriptionController controller;
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
			if (GUILayout.Button("Begin Selecting"))
			{
				isSelecting = true;
				controller = (SubscriptionController)target;
			}
		}
		else
		{
			if (GUILayout.Button("Stop Selecting"))
			{
				isSelecting = false;
				controller = null;
			}
		}
	}

	void OnScene(SceneView scene)
	{
		//skip if not selecting
		if (!isSelecting || controller == null)
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

			var hashHits = new HashSet<GameObject>(hits.Select(x => x.transform.gameObject));

			var objectsToDirt = controller.SubscribeToController(hashHits);

			EditorUtility.SetDirty(controller);
			foreach (var objectToDirt in objectsToDirt)
			{
				EditorUtility.SetDirty(objectToDirt);
			}

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}

		//return selection to switch
		Selection.activeGameObject = controller.gameObject;
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
