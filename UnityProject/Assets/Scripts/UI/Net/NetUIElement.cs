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

	/// Should client attempts to change this be ignored?
	public virtual bool IsNonInteractable => false;

	/// Server-only method for updating element (i.e. changing label text) from server GUI code
	public string NewValue {
		set {
			Value = value;
			UpdatePeepers();
		}
	}

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
	protected void UpdatePeepers() {
		TabUpdateMessage.SendToPeepers( MasterTab.Provider, MasterTab.Type, TabAction.Update, new[] {ElementValue} );
	}
	public abstract void ExecuteServer();

	public override string ToString() {
		return ElementValue.ToString();
	}
}
