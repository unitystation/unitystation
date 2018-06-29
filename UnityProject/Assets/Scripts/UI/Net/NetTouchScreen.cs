using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Util;

[RequireComponent(typeof( TouchScreen ))]
[Serializable]
public class NetTouchScreen : NetUIElement
{
	public override ElementMode InteractionMode => ElementMode.ClientWrite;

	public override string Value {
		set { Element.LastTouchPosition = value.Vectorized(); }
		get { return Element.LastTouchPosition.Stringified(); }
	}

	private TouchScreen element;
	public TouchScreen Element {
		get {
			if ( !element ) {
				element = GetComponent<TouchScreen>();
			}
			return element;
		}
	}
	
	public StringEvent ServerMethod;
	
	public override void ExecuteServer() {
		ServerMethod.Invoke(Value);
	}
}
