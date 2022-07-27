using Core;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomInspectors
{
	[CustomEditor(typeof(Rotatable), true)]
	public class RotatableInspector : Editor
	{
		private Rotatable thisDevice;

		private float closestMasterDistance = -2;

		private void OnEnable()
		{
			thisDevice = (Rotatable) target;
		}

		public override void OnInspectorGUI()
		{
			// Render default inspector for the component.
			base.OnInspectorGUI();

			// Don't show connection elements; not relevant to runtime or prefab edit mode.
			if (Application.isPlaying || PrefabStageUtility.GetCurrentPrefabStage() != null) return;

			GUILayout.Label("Set Direction");

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Up"))
			{
				thisDevice.FaceDirection(OrientationEnum.Up_By0);
				Save();
			}

			if (GUILayout.Button("Right"))
			{
				thisDevice.FaceDirection(OrientationEnum.Right_By270);
				Save();
			}

			if (GUILayout.Button("Down"))
			{
				thisDevice.FaceDirection(OrientationEnum.Down_By180);
				Save();
			}

			if (GUILayout.Button("Left"))
			{
				thisDevice.FaceDirection(OrientationEnum.Left_By90);
				Save();
			}

			GUILayout.EndHorizontal();

			if (GUILayout.Button("Refresh"))
			{
				thisDevice.Refresh();
				Save();
			}
		}

		private void Save()
		{
			EditorUtility.SetDirty((Component) thisDevice);
			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}
	}
}