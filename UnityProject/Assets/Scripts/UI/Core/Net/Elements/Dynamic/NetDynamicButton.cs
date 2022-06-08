using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// Uses entry name as value to be passed in methods
	/// Intended for dynamic lists
	[RequireComponent(typeof(Button))]
	[Serializable]
	public class NetDynamicButton : NetUIStringElement
	{
		public override string Value => gameObject.transform.parent.gameObject.name;

		public StringEvent ServerMethod;

		public override void Init()
		{
			//Cleaning out old listeners in case of reuse
			ServerMethod.RemoveAllListeners();

			if (ServerMethod.GetPersistentEventCount() == 0) return;

			//some reflection is required here.

			//reading prefab-based listener information:
			var tabToLookup = ServerMethod.GetPersistentTarget(0);
			MethodInfo methodInfo = UnityEventBase.GetValidMethodInfo(tabToLookup, ServerMethod.GetPersistentMethodName(0), new[] { typeof(string) });

			//disabling rigid listener
			ServerMethod.SetPersistentListenerState(0, UnityEventCallState.Off);

			//looking up a live instance of that tab we initially mapped listener to (in editor)
			var foundComponent = GetComponentInParent(tabToLookup.GetType());

			//making a dynamic copy of initial prefab-based listener
			UnityAction<string> execute = str => methodInfo.Invoke(foundComponent, new object[] { str });

			//applying a dynamic copy of initial prefab-based listener
			ServerMethod.AddListener(execute);
		}

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke(Value);
		}
	}
}
