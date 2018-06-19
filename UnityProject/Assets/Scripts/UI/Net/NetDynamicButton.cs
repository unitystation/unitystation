using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
/// Uses entry name as value to be passed in methods
/// Intended for dynamic lists
[RequireComponent(typeof( Button ))]
[Serializable]
public class NetDynamicButton : NetUIElement
{
	public override string Value {
		get { return gameObject.transform.parent.gameObject.name; }
	}
	public StringEvent ServerMethod;

	public override void Init() {
		//some reflection is required here.
		//grabbing tab prefab w/ method name and looking it up in parents
		var tabToLookup = ServerMethod.GetPersistentTarget( 0 );
		MethodInfo methodInfo = UnityEventBase.GetValidMethodInfo( tabToLookup,
			ServerMethod.GetPersistentMethodName( 0 ),
			new[]{typeof(string)} );
		UnityEditor.Events.UnityEventTools.RemovePersistentListener( ServerMethod, 0 );
		 
		var foundComponent = GetComponentInParent(tabToLookup.GetType());
		UnityAction<string> execute = str => methodInfo.Invoke(foundComponent, new object[] {str});
		
//		UnityEditor.Events.UnityEventTools.AddPersistentListener( ServerMethod, execute ); //todo investigate 
		ServerMethod.AddListener (execute);
	}

	public override void ExecuteServer() {
		ServerMethod.Invoke(Value);
	}

}
