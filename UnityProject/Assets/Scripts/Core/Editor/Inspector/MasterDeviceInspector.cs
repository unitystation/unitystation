using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Core.Editor.Tools.Mapping;
using Systems.ObjectConnection;

using Objects.Atmospherics;
using Objects.Engineering;
using Objects.Lighting;
using Objects.Wallmounts;
using Objects.Wallmounts.Switches;


namespace CustomInspectors
{
	/// <summary>
	/// Simply draws a line between the master device and its linked slave devices for assisted mapping.
	/// </summary>
	[CustomEditor(typeof(IMultitoolMasterable), true)]
	public class MasterDeviceInspector : Editor
	{
		public static Dictionary<MultitoolConnectionType, Color> LinkColors = new Dictionary<MultitoolConnectionType, Color>()
		{
			{ MultitoolConnectionType.APC, new Color(0.5f, 0.5f, 1, 1) },
			{ MultitoolConnectionType.Acu, Color.cyan },
			{ MultitoolConnectionType.FireAlarm, new Color(1, 0.5f, 0, 1) },
			{ MultitoolConnectionType.LightSwitch, Color.yellow },
		};

		private void OnEnable()
		{
			DeviceLinker.InitDeviceLists(((IMultitoolMasterable) target).ConType, forceRefresh: true);
		}

		[DrawGizmo(GizmoType.Selected | GizmoType.Active)]
		private static void DrawGizmoConnection(IMultitoolMasterable device, GizmoType type)
		{
			DeviceLinker.InitDeviceLists(device.ConType);

			Gizmos.color = LinkColors.TryGetValue(device.ConType, out var color) ? color : Color.green;

			foreach (IMultitoolSlaveable slave in DeviceLinker.Slaves)
			{
				if (slave.Master != device) continue;

				GizmoUtils.DrawArrow(
						device.gameObject.transform.position,
						slave.gameObject.transform.position - device.gameObject.transform.position,
						false);
				
			}

			Gizmos.DrawSphere(device.gameObject.transform.position, 0.15f);
		}
	}

	#region Hack
	// Because [CustomEditor(typeof(IMultitoolSlaveable), true)] just isn't enough... plz Unity, plz.

	[CustomEditor(typeof(APC))]
	public class ApcInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(AirController))]
	public class AcuInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(FireAlarm))]
	public class FireAlarmInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(GeneralSwitch))]
	public class GeneralSwitchInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(LightSwitchV2))]
	public class LightSwitchInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(TurretSwitch))]
	public class TurretSwitchInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(DoorSwitch))]
	public class DoorSwitchInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(StatusDisplay))]
	public class StatusDisplayInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(ReactorGraphiteChamber))]
	public class ReactorGraphiteChamberInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(ReactorBoiler))]
	public class ReactorBoilerInspector : MasterDeviceInspector { }

	[CustomEditor(typeof(ReactorTurbine))]
	public class ReactorTurbineMasterInspector : MasterDeviceInspector { }

	#endregion
}
