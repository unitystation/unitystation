#if UNITY_EDITOR
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using Core.Editor.Tools.Mapping;
using Systems.ObjectConnection;

namespace CustomInspectors
{
	/// <summary>
	/// Simply draws a line between the master device and its linked slave devices for assisted mapping.
	/// </summary>
	public class MasterDeviceInspector : InterfaceEditor
	{
		public static Dictionary<MultitoolConnectionType, Color> LinkColors = new Dictionary<MultitoolConnectionType, Color>()
		{
			{ MultitoolConnectionType.APC, new Color(0.5f, 0.5f, 1, 1) },
			{ MultitoolConnectionType.Acu, Color.cyan },
			{ MultitoolConnectionType.FireAlarm, new Color(1, 0.5f, 0, 1) },
			{ MultitoolConnectionType.LightSwitch, Color.yellow },
			{ MultitoolConnectionType.Artifact, Color.magenta },
		};

		public override void OnEnableInEditor(object target)
		{
			var asd = (IMultitoolMasterable)target;
			DeviceLinker.InitDeviceLists(asd.ConType, true);
		}

		[DrawGizmo(GizmoType.Selected | GizmoType.Active)]
		public static void DrawGizmoConnectionCall(IMultitoolMasterable device, GizmoType type)
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
}
#endif
