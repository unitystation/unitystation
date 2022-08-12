#if UNITY_EDITOR
using System.Collections.Generic;
using Shared.Editor;
using Shared.Systems.ObjectConnection;
using UnityEditor;
using UnityEngine;

namespace CustomInspectors
{
	/// <summary>
	/// Simply draws a line between the master device and its linked slave devices for assisted mapping.
	/// </summary>
	public class MasterDeviceInspector : InterfaceEditor
	{
		public static Dictionary<MultitoolConnectionType, Color> LinkColors = new()
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
			var position = device.gameObject.transform.position;
			DeviceLinker.InitDeviceLists(device.ConType);

			Gizmos.color = LinkColors.TryGetValue(device.ConType, out var color) ? color : Color.green;

			foreach (var slave in DeviceLinker.Slaves)
			{
				if (slave.Master != device) continue;

				GizmoUtils.DrawArrow(position, slave.gameObject.transform.position - position, false);
			}

			Gizmos.DrawSphere(position, 0.15f);
		}
	}
}
#endif
