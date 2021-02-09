using UnityEngine;
using UnityEngine.UI;

public class NetRadiusCircle : NetUIStringElement
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;
	public override string Value {
		get { return radius.ToString(); }
		set {
			externalChange = true;
			//don't update if it's the same sprite
			if ( radius.ToString() != value ) {
				int parsedValue;
				if ( int.TryParse( value, out parsedValue ) ) {
					radius = parsedValue;

					//Not showing circle for invalid radii
					if ( radius < 1 ) {
						Color modifiedColor = Element.color;
						modifiedColor.a = 0;
						Element.color = modifiedColor;
					} else {
						float scale = Mathf.Clamp( radius / 10, 1, 10 );
						transform.localScale = new Vector3( scale, scale, 1 );

						//making larger ones more transparent
						Color modifiedColor = Element.color;
						modifiedColor.a = 1 / scale;
						Element.color = modifiedColor;
					}
				}
			}
			externalChange = false;
		}
	}
	private int radius = -1;
	private Image element;
	public Image Element {
		get {
			if ( !element ) {
				element = GetComponent<Image>();
			}
			return element;
		}
	}

	public override void ExecuteServer(ConnectedPlayer subject) {}
}