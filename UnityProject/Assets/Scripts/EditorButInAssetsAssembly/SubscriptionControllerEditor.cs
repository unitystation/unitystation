#if UNITY_EDITOR

using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Shared.Systems.ObjectConnection;

namespace CustomInspectors
{
	public class SubscriptionControllerEditor : InterfaceEditor
	{
		private GameObject gameObject;
		private ISubscriptionController controller;
		private bool isSelecting;

		public override void OnEnableInEditor(object target)
		{
			SceneView.duringSceneGui += OnScene;
		}

		public override void OnDisableInEditor(object target)
		{
			SceneView.duringSceneGui -= OnScene;
		}

		public override void OnInspectorGUIInEditor(Component target)
		{
			if (!isSelecting)
			{
				if (GUILayout.Button("Begin Selecting"))
				{
					isSelecting = true;
					gameObject = target.gameObject;
					controller = target.GetComponent<ISubscriptionController>();
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

				EditorUtility.SetDirty(controller as MonoBehaviour);
				Undo.RecordObject(controller as MonoBehaviour, "Linking device");
				EditorUtility.SetDirty(gameObject);
				Undo.RecordObject(gameObject, "Linking device");
				foreach (var objectToDirt in objectsToDirt)
				{
					//make that shit dirty
					EditorUtility.SetDirty(objectToDirt);
					Undo.RecordObject(objectToDirt, "Linking device");

					//make all that shit's shit dirty because fuck you unity
					foreach (var component in objectToDirt.GetComponents<Component>())
					{
						EditorUtility.SetDirty(component);
						Undo.RecordObject(component, "Linking device");
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
#endif
