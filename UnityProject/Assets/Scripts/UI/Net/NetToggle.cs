using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
/// Toggle for bool-based methods
/// fixme could be some issues with initially inverted state
[RequireComponent(typeof(Toggle))]
[Serializable]
public class NetToggle : NetUIElement
{
	public override string Value {
		get { return Element.isOn ? "1" : "0"; }
		set {
			externalChange = true;
			Element.isOn = value.Equals("1");
			externalChange = false;
		}
	}

	public BoolEvent ServerMethod;

	private Toggle element;

	public override void ExecuteServer() {
		ServerMethod.Invoke(Element.isOn);
	}

	public Toggle Element {
		get {
			if ( !element ) {
				element = GetComponent<Toggle>();
			}
			return element;
		}
	}
}
/// <inheritdoc />
/// "If you wish to use a generic UnityEvent type you must override the class type."
[Serializable]
public class BoolEvent : UnityEvent<bool>
{
}