using System;
using Systems.ObjectConnection;
using Mirror;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace CustomInspectors
{
	[ExecuteInEditMode]
	public class InterfaceGUI : NetworkBehaviour
	{
		public List<InterfaceEditor> runningInterfaces = new List<InterfaceEditor>();
		public virtual void Update()
		{
#if UNITY_EDITOR
			runningInterfaces.Clear();
			var objType = GetType();
			Type[] interfaceList = objType.GetInterfaces();
			foreach (var interfaceType in interfaceList)
			{
				if (interfaceType == typeof(IMultitoolMasterable))
				{
					runningInterfaces.Add(new MasterDeviceInspector());
				}
				else if (interfaceType == typeof(SubscriptionController))
				{
					runningInterfaces.Add(new SubscriptionControllerEditor());
				}
			}
#endif
		}
	}

	[CustomEditor(typeof(InterfaceGUI), true)]
	public class InterfaceGUIEditor : Editor
	{
		private InterfaceGUI interfaceGUI;


		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			foreach (var interfaceEntry in interfaceGUI.runningInterfaces)
			{
				interfaceEntry.OnInspectorGUICALL(target);
			}
		}

		private void OnEnable()
		{
			interfaceGUI = (InterfaceGUI)target;

			foreach (var interfaceEntry in interfaceGUI.runningInterfaces)
			{
				interfaceEntry.OnEnableCALL(target);
			}
		}

		private void OnDisable()
		{
			foreach (var interfaceEntry in interfaceGUI.runningInterfaces)
			{
				interfaceEntry.OnDisableCALL(target);
			}
		}

		[DrawGizmo(GizmoType.Selected | GizmoType.Active)]
		private void DrawGizmoConnection(IMultitoolMasterable device, GizmoType type)
		{
			foreach (var interfaceEntry in interfaceGUI.runningInterfaces)
			{
				interfaceEntry.DrawGizmoConnectionCALL(device, type);
			}
		}
	}

	public class InterfaceEditor : Editor
	{
		public virtual void OnEnableCALL(object target) { }
		public virtual void OnDisableCALL(object target) { }
		public virtual void OnInspectorGUICALL(object target) { }
		public void DrawGizmoConnectionCALL(object target, GizmoType type) { }
	}

}


