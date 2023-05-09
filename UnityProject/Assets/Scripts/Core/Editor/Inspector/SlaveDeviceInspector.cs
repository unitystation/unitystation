using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Doors;
using Objects.Atmospherics;
using Objects.Engineering;
using Objects.Engineering.Reactor;
using Objects.Lighting;
using Objects.Other;
using Shared.Editor;
using Shared.Systems.ObjectConnection;
using Systems.Electricity;


namespace CustomInspectors
{
	/// <summary>
	/// <para>Allows the mapper to conveniently link a slave device to a master device, and find disconnected slave devices.</para>
	/// </summary>
	[CustomEditor(typeof(IMultitoolSlaveable), true)]
	public class SlaveDeviceInspector : Editor
	{
		private static readonly Dictionary<MultitoolConnectionType, string> disconnectedGizmoIcons = new Dictionary<MultitoolConnectionType, string>
		{
			{ MultitoolConnectionType.APC, "disconnected" },
			{ MultitoolConnectionType.DoorButton, "noDoor" },
		};

		private IMultitoolSlaveable thisDevice;

		private float closestMasterDistance = -1;

		private void OnEnable()
		{
			thisDevice = (IMultitoolSlaveable) target;

			DeviceLinker.InitDeviceLists(thisDevice.ConType, forceRefresh: true);
		}

		[DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.NonSelected)]
		private static void DrawGizmoConnection(IMultitoolSlaveable device, GizmoType type)
		{
			if (PrefabStageUtility.GetCurrentPrefabStage() != null) return; // Don't show in prefab mode.

			if (type.HasFlag(GizmoType.Selected) || type.HasFlag(GizmoType.Active))
			{
				if (device.Master == null) return;

				Gizmos.color = MasterDeviceInspector.LinkColors.TryGetValue(device.ConType, out var color) ? color : Color.green;
				GizmoUtils.DrawArrow(
						device.Master.gameObject.transform.position,
						device.gameObject.transform.position - device.Master.gameObject.transform.position,
						false);
				Gizmos.DrawSphere(device.Master.gameObject.transform.position, 0.15f);
			}
			else if (type.HasFlag(GizmoType.NonSelected))
			{
				if (device.RequireLink == false || device.Master != null) return;

				string icon = disconnectedGizmoIcons.TryGetValue(device.ConType, out var filename) ? filename : "no-wifi";
				Gizmos.DrawIcon(device.gameObject.transform.position, icon);
			}
		}

		public override void OnInspectorGUI()
		{
			// Render default inspector for the component.
			base.OnInspectorGUI();

			EditorGUILayout.HelpBox("You can connect all slave devices at once via\n`Mapping/Device Linker`.", MessageType.Info);

			// Don't show connection elements; not relevant to runtime or prefab edit mode.
			if (Application.isPlaying || PrefabStageUtility.GetCurrentPrefabStage() != null) return;

			DeviceLinker.InitDeviceLists(thisDevice.ConType);

			if (thisDevice.RequireLink && thisDevice.Master == null)
			{
				EditorGUILayout.HelpBox("Not connected to any master device!", MessageType.Warning);
			}

			GUILayout.Label("Connect to a master device:");

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Closest"))
			{
				closestMasterDistance = DeviceLinker.TryLinkSlaveToClosestMaster(thisDevice);
				Save();
			}
			if (thisDevice.Master != null)
			{
				if (GUILayout.Button("Closer"))
				{
					DeviceLinker.TryLinkToNextMaster(thisDevice, -1);
					Save();
				}
				else if (GUILayout.Button("Further"))
				{
					DeviceLinker.TryLinkToNextMaster(thisDevice, 1);
					Save();
				}
			}
			GUILayout.EndHorizontal();

			if (closestMasterDistance == -1)
			{
				GUILayout.Label($"No master devices found!", EditorUIUtils.LabelStyle);
			}
			else if (closestMasterDistance > DeviceLinker.Masters[0].MaxDistance)
			{
				GUILayout.Label($"Closest master device is <b>{closestMasterDistance, 0:N}</b> tiles away, " +
						$"but this exceeds the maximum distance of <b>{DeviceLinker.Masters[0].MaxDistance}</b>!", EditorUIUtils.LabelStyle);
			}

			if (thisDevice.Master != null)
			{
				GUILayout.BeginHorizontal();
				var distance = Vector3.Distance(thisDevice.gameObject.transform.position, thisDevice.Master.gameObject.transform.position);
				GUILayout.Label($"Connected to <b>{thisDevice.Master.gameObject.name}</b> " +
						$"(distance of <b>{distance, 0:N}</b> tiles).", EditorUIUtils.LabelStyle);
				if (GUILayout.Button("Clear", GUILayout.Width(EditorGUIUtility.currentViewWidth / 4)))
				{
					thisDevice.SetMasterEditor(null);
					Save();
				}
				GUILayout.EndHorizontal();
			}
		}

		private void Save()
		{
			Undo.RecordObject ((Component) thisDevice, "Save");
			EditorUtility.SetDirty((Component) thisDevice);
			if (thisDevice.Master != null)
			{
				Undo.RecordObject ((Component) thisDevice.Master, "Save");
				EditorUtility.SetDirty((Component) thisDevice.Master);
			}
			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		}
	}

	#region Hack
	// Because [CustomEditor(typeof(IMultitoolSlaveable), true)] just isn't enough... plz Unity, plz.

	[CustomEditor(typeof(APCPoweredDevice))]
	public class ApcDeviceInspector : SlaveDeviceInspector { }

	[CustomEditor(typeof(AcuDevice))]
	public class AcuDeviceInspector : SlaveDeviceInspector { }

	[CustomEditor(typeof(FireLock))]
	public class FirelockInspector : SlaveDeviceInspector { }

	[CustomEditor(typeof(LightSource))]
	public class LightSourceInspector : SlaveDeviceInspector { }

	[CustomEditor(typeof(Turret))]
	public class TurretInspector : SlaveDeviceInspector { }

	[CustomEditor(typeof(DoorController))]
	public class DoorControllerInspector : SlaveDeviceInspector { }

	[CustomEditor(typeof(ReactorControlConsole))]
	public class ReactorControlConsoleInspector : SlaveDeviceInspector { }

	[CustomEditor(typeof(BoilerTurbineController))]
	public class BoilerTurbineControllerInspector : SlaveDeviceInspector { }

	[CustomEditor(typeof(ReactorTurbine))]
	public class ReactorTurbineSlaveInspector : SlaveDeviceInspector { }

	#endregion
}
