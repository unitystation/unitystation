using System;
using Systems.ObjectConnection;
using Mirror;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace CustomInspectors
{
	/// <summary>
	/// Finds C# interfaces in its inheritor and manually assigns a unity editor GUI for the inspector if appropriate
	/// </summary>
	[ExecuteInEditMode]
	public class InterfaceGUI : NetworkBehaviour
	{
		public List<InterfaceEditor> runningInterfaces = new List<InterfaceEditor>();
		public virtual void OnEnable()
		{
#if UNITY_EDITOR
			runningInterfaces.Clear();
			var objType = GetType();
			Type[] interfaceList = objType.GetInterfaces();
			foreach (var interfaceType in interfaceList)
			{
				if (interfaceType == typeof(IMultitoolMasterable))
				{
					runningInterfaces.Add(ScriptableObject.CreateInstance<MasterDeviceInspector>());
				}
				else if (interfaceType == typeof(SubscriptionController))
				{
					runningInterfaces.Add(ScriptableObject.CreateInstance<SubscriptionControllerEditor>());
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
				interfaceEntry.OnInspectorGUIInEditor(target);
			}
		}

		private void OnEnable()
		{
			interfaceGUI = (InterfaceGUI)target;

			foreach (var interfaceEntry in interfaceGUI.runningInterfaces)
			{
				interfaceEntry.OnEnableInEditor(target);
			}
		}

		private void OnDisable()
		{
			foreach (var interfaceEntry in interfaceGUI.runningInterfaces)
			{
				interfaceEntry.OnDisableInEditor(target);
			}
		}

		[DrawGizmo(GizmoType.Selected | GizmoType.Active)]
		private void DrawGizmoConnection(IMultitoolMasterable device, GizmoType type)
		{
			foreach (var interfaceEntry in interfaceGUI.runningInterfaces)
			{
				interfaceEntry.DrawGizmoConnectionInEditor(device, type);
			}
		}
	}

	public class InterfaceEditor : Editor
	{
		public virtual void OnEnableInEditor(object target) { }
		public virtual void OnDisableInEditor(object target) { }
		public virtual void OnInspectorGUIInEditor(object target) { }
		public void DrawGizmoConnectionInEditor(object target, GizmoType type) { }
	}

}


