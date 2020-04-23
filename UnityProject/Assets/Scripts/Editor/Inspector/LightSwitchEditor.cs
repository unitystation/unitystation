using System.Linq;
using Lighting;
using Mirror;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Objects;

[CustomEditor(typeof(LightSwitchV2),true )]
public class LightSwitchEditor : Editor
{
	private LightSwitchV2 switchBase;
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
				switchBase = (LightSwitchV2)target;
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
				var objectTrigger = hits[i].transform.GetComponent<LightSource>();
				if (objectTrigger != null)
					ToggleObjectTrigger(switchBase, objectTrigger);
			}

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}

		//return selection to switch
		Selection.activeGameObject = switchBase.gameObject;
	}

	private void ToggleObjectTrigger(LightSwitchV2 lightSwitch, LightSource lightSource)
	{
		if (lightSwitch.listOfLights.Contains(lightSource))
		{
			lightSwitch.listOfLights.Remove(lightSource);
			lightSource.relatedLightSwitch = null;

			EditorUtility.SetDirty(lightSource);
			EditorUtility.SetDirty(lightSwitch);
		}
		else
		{
			lightSwitch.listOfLights.Add(lightSource);
			lightSource.relatedLightSwitch = lightSwitch;

			EditorUtility.SetDirty(lightSource);
			EditorUtility.SetDirty(lightSwitch);
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

