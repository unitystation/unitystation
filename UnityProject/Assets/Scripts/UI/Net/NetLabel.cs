using System;
using UnityEngine;
using UnityEngine.UI;
///Text label, not modifiable by clients directly
[RequireComponent(typeof(Text))]
[Serializable]
public class NetLabel : NetUIElement
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	public override string Value {
		get { return Element.text; }
		set {
			externalChange = true;
			Element.text = value;
			externalChange = false;
		}
	}

	private Text element; 
	public Text Element {
		get {
			if ( !element ) {
				element = GetComponent<Text>();
			}
			return element;
		}
	}

	public override void ExecuteServer() {	}
}