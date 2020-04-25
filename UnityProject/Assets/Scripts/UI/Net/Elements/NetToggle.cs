using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Toggle for bool-based methods
[RequireComponent(typeof(Toggle))]
[Serializable]
public class NetToggle : NetUIStringElement
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
	public BoolEventWithSubject ServerMethodWithSubject;

	private Toggle element;

	public override void ExecuteServer(ConnectedPlayer subject) {
		ServerMethod?.Invoke(Element.isOn);
		ServerMethodWithSubject?.Invoke(Element.isOn, subject);
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
public class BoolEvent : UnityEvent<bool>{}

[Serializable]
public class BoolEventWithSubject : UnityEvent<bool, ConnectedPlayer>{}