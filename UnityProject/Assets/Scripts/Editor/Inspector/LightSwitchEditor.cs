/*using System.Linq;
using Lighting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


	[CustomEditor(typeof(LightSwitchV2))]
	public class LightSwitchEditor : Editor
	{
		private LightSwitchV2 lightSwtich;
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
				if (GUILayout.Button("Begin Selecting Lights"))
				{
					isSelecting = true;
					lightSwtich = (LightSwitchV2) target;
				}
			}
			else
			{
				if (GUILayout.Button("Stop Selecting Lights"))
				{
					isSelecting = false;
					lightSwtich = null;
				}
			}
		}

		void OnScene(SceneView scene)
		{
			//skip if not selecting
			if (!isSelecting || lightSwtich == null)
				return;

			Event e = Event.current;
			if (e == null)
				return;

			if (HasPressedEscapeKey(e))
			{
				//Undo.RecordObject(lightSwtich,);
				isSelecting = false;
				return;
			}

			if (HasPressedLeftClick(e))
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
				RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

				// scan all hit objects for ObjectTrigger
				for (int i = 0; i < hits.Length; i++)
				{
					var lightSource = hits[i].transform.GetComponent<LightSource>();
					if (lightSource != null)
						ToggleLightSource(lightSwtich, lightSource);
				}

				EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
			}

			//return selection to switch
			Selection.activeGameObject = lightSwtich.gameObject;
		}

		private void ToggleLightSource(LightSwitchV2 lightSwitch, LightSource lightSource)
		{
			if (lightSwitch.listLightSources.Contains(lightSource))
			{
				lightSwitch.listLightSources.Remove(lightSource);
				EditorUtility.SetDirty(lightSwitch);
			}
			else
			{
				lightSwitch.listLightSources.Add(lightSource);
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
	}*/