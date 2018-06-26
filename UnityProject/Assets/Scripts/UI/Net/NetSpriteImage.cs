using System;
using UnityEngine;
using UnityEngine.UI;

/// Holds value of what sprite to load
/// sprite-based
public class NetSpriteImage : NetUIElement
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;
	public override string Value {
		get { return spriteName ?? "-1"; }
		set {
			externalChange = true;
			var split = value.Split( new []{'_'} , StringSplitOptions.RemoveEmptyEntries );
			switch ( split.Length ) {
				case 0:
					//don't load anything
					break;
				case 1:
					//load entire sprite//todo
					break;
				default:
					//load sub-sprite from sheet
					Sprite[] spriteSheet = Resources.LoadAll<Sprite>( split[0] );
					int index;
					if ( int.TryParse( split[1], out index ) && spriteSheet?.Length > 0 ) {
						Element.sprite = spriteSheet[index];
						spriteName = value;
					} else {
						Debug.LogWarning( $"Unable to load sprite '{split[0]}'" );
					}

					break;
			}
			externalChange = false;
		}
	}
	private Image element;
	private string spriteName;
	public Image Element {
		get {
			if ( !element ) {
				element = GetComponent<Image>();
			}
			return element;
		}
	}
	
	public override void ExecuteServer() {}
}