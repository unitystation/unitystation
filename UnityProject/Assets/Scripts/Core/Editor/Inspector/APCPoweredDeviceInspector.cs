using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Systems.Electricity;
using Objects.Engineering;

namespace CustomInspectors
{
	[CustomEditor(typeof(APCPoweredDevice))]
	public class APCPoweredDeviceEditor: Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (Application.isPlaying)
			{
				return;
			}

			var device = (APCPoweredDevice) target;

			if (device.ConType != MultitoolConnectionType.APC) return;

			if (!device.IsSelfPowered)
			{
				if (!device.IsSelfPowered && device.RelatedAPC == null)
				{
					GUILayout.Label("NOT CONNECTED TO ANY APC", "warning");
				}
				else
				{
					GUILayout.Label($"Connected to APC: {device.RelatedAPC.gameObject.name}");
				}
			}

			if (GUILayout.Button("Auto connect to APC"))
			{
				ConnectToClosestAPC(device);
			}
		}

		private void ConnectToClosestAPC(APCPoweredDevice device)
		{
			var apcs = FindObjectsOfType<APC>();

			if (apcs.Length == 0)
			{
				return;
			}

			APC bestTarget = null;
			float closestDistance = Mathf.Infinity;
			var devicePosition = device.gameObject.transform.position;

			foreach (var potentialTarget in apcs)
			{
				var directionToTarget = potentialTarget.gameObject.transform.position - devicePosition;
				float dSqrToTarget = directionToTarget.sqrMagnitude;

				if (dSqrToTarget >= closestDistance) continue;
				closestDistance = dSqrToTarget;
				bestTarget = potentialTarget;
			}

			if (bestTarget == null || bestTarget == device.RelatedAPC) return;
			device.RelatedAPC = bestTarget;

			EditorUtility.SetDirty(device);
			EditorUtility.SetDirty(device.RelatedAPC);
			bestTarget.AddDevice(device);
			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}
	}
}
