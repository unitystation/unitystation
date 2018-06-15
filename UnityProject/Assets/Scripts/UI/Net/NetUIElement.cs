using System;
using UnityEngine;
[Serializable]
public abstract class NetUIElement : MonoBehaviour
{
	private NetUITab masterTab;
	protected bool externalChange;

	public NetUITab MasterTab {
		get {
			if ( !masterTab ) {
				masterTab = GetComponentsInParent<NetUITab>(true)[0];
			}
			return masterTab;
		}
	}

	public ElementValue ElementValue => new ElementValue{Id = name, Value = Value};

	public virtual ElementMode InteractionMode => ElementMode.Normal;

	/// Server-only method for updating element (i.e. changing label text) from server GUI code
	public virtual string SetValue {
		set {
			Value = value;
			UpdatePeepers();
		}
	}

	/// Initialize method before element list is collected. For editor-set values
	public virtual void Init() {}

	public virtual string Value {
		get {
			return "-1";
		}
		set {
		}
	}

	public virtual void ExecuteClient() {
		//Don't send if triggered by external change
		if ( !externalChange ) {
			TabInteractMessage.Send(MasterTab.Provider, MasterTab.Type, name, Value);
		}
	}
	protected virtual void UpdatePeepers() {
		TabUpdateMessage.SendToPeepers( MasterTab.Provider, MasterTab.Type, TabAction.Update, new[] {ElementValue} );
	}
	public abstract void ExecuteServer();

	public override string ToString() {
		return ElementValue.ToString();
	}
}

public enum ElementMode {
	/// Changeable by both client and server
	Normal, 
	/// Only server can change value
	ServerWrite, 
	/// Only client can change value, server doesn't store it
	ClientWrite
}