using System;
using Systems.ObjectConnection;
using Mirror;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

namespace CustomInspectors
{
	/// <summary>
	/// Finds C# interfaces in its inheritor and manually assigns a unity editor GUI for the inspector if appropriate
	/// </summary>
	[ExecuteInEditMode]
	public class ImnterfaceMultitoolGUI : NetworkBehaviour
	{
#if UNITY_EDITOR
		public List<InterfaceEditor> runningInterfaces = new List<InterfaceEditor>();

		public virtual void OnEnable()
		{
			runningInterfaces.Clear();
			var objType = GetType();
			Type[] interfaceList = objType.GetInterfaces();
			foreach (var interfaceType in interfaceList)
			{
				if (interfaceType == typeof(IMultitoolMasterable))
				{
					runningInterfaces.Add(ScriptableObject.CreateInstance<MasterDeviceInspector>());
				}
				else if (interfaceType == typeof(ISubscriptionController))
				{
					runningInterfaces.Add(ScriptableObject.CreateInstance<SubscriptionControllerEditor>());
				}
			}

		}
#endif
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(ImnterfaceMultitoolGUI), true)]
	public class InterfaceGUIEditor : Editor
	{
		private ImnterfaceMultitoolGUI imnterfaceMultitoolGUI;


		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			foreach (var interfaceEntry in imnterfaceMultitoolGUI.runningInterfaces)
			{
				interfaceEntry.OnInspectorGUIInEditor((Component)target);
			}
		}

		private void OnEnable()
		{
			imnterfaceMultitoolGUI = (ImnterfaceMultitoolGUI)target;

			foreach (var interfaceEntry in imnterfaceMultitoolGUI.runningInterfaces)
			{
				interfaceEntry.OnEnableInEditor(target);
			}
		}

		private void OnDisable()
		{
			foreach (var interfaceEntry in imnterfaceMultitoolGUI.runningInterfaces)
			{
				interfaceEntry.OnDisableInEditor(target);
			}
		}

		[DrawGizmo(GizmoType.Selected | GizmoType.Active)]
		private static void DrawGizmoConnection(IMultitoolMasterable device, GizmoType type)
		{
			foreach (var interfaceEntry in device.gameObject.GetComponent<ImnterfaceMultitoolGUI>().runningInterfaces)
			{
				interfaceEntry.DrawGizmoConnectionInEditor(device, type);
			}
		}
	}

	public class InterfaceEditor : Editor
	{
		public virtual void OnEnableInEditor(object target) { }
		public virtual void OnDisableInEditor(object target) { }
		public virtual void OnInspectorGUIInEditor(Component target) { }
		public void DrawGizmoConnectionInEditor(object target, GizmoType type) { }
	}
#endif
}


