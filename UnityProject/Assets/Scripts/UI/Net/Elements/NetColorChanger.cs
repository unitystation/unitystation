using System;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Slap this on image/text label gameobject to control its color
/// </summary>
[RequireComponent(typeof(Graphic))]
[Serializable]
public class NetColorChanger : NetUIElement
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	public override string Value {
		get { return DebugTools.ColorToHex(Element.color); }
		set {
			externalChange = true;
			Element.color = DebugTools.HexToColor( value );
			externalChange = false;
		}
	}

	private Graphic element; 
	public Graphic Element {
		get {
			if ( !element ) {
				element = GetComponent<Graphic>();
			}
			return element;
		}
	}

	public override void ExecuteServer() {	}
}