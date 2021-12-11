﻿using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Systems.ObjectConnection;

namespace CustomInspectors
{
	public class SubscriptionControllerEditor : InterfaceEditor
	{
		private GameObject gameObject;
		private SubscriptionController controller;
		private bool isSelecting;

		public override void OnEnableCALL(object target)
		{
			SceneView.duringSceneGui += OnScene;
		}

		public override void OnDisableCALL(object target)
		{
			SceneView.duringSceneGui -= OnScene;
		}

		public override void OnInspectorGUICALL(object target)
		{
			if (!isSelecting)
			{
				if (GUILayout.Button("Begin Selecting"))
				{
					isSelecting = true;
					gameObject = (GameObject) target;
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

			UnityEngine.Event e = UnityEngine.Event.current;
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

				var objectHitRaycast = hits.Select(hit => hit.collider.gameObject).ToList();

				var objectsToDirt = controller.SubscribeToController(objectHitRaycast);

				EditorUtility.SetDirty(gameObject);
				foreach (var objectToDirt in objectsToDirt)
				{
					//make that shit dirty
					EditorUtility.SetDirty(objectToDirt);

					//make all that shit's shit dirty because fuck you unity
					foreach (var component in objectToDirt.GetComponents<Component>())
					{
						EditorUtility.SetDirty(component);
					}
				}

				EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
			}

			//return selection to switch
			Selection.activeGameObject = gameObject.gameObject;
		}

		private bool HasPressedEscapeKey(UnityEngine.Event e)
		{
			return e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape;
		}

		private bool HasPressedLeftClick(UnityEngine.Event e)
		{
			return e.type == EventType.MouseDown && e.button == 0;
		}
	}
}

