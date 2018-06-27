using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Holds value of what sprite to load
/// sprite-based
public class NetSpriteImage : NetUIElement
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;
	public static Dictionary<string, Sprite[]> Sprites = new Dictionary<string, Sprite[]>();
	public override string Value {
		get { return spriteName ?? "-1"; }
		set {
			externalChange = true;
			//don't update if it's the same sprite
			if ( spriteName != value ) {
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
						string spriteFile = split[0];
						if ( !Sprites.ContainsKey( spriteFile ) ) {
							Sprites.Add( spriteFile, Resources.LoadAll<Sprite>( spriteFile ) );
						}
	
						var spriteSheet = Sprites[spriteFile];
						int index;
						if ( int.TryParse( split[1], out index ) && spriteSheet?.Length > 0 ) {
							Element.sprite = spriteSheet[index];
							spriteName = value;
						} else {
							Debug.LogWarning( $"Unable to load sprite '{spriteFile}'" );
						}
						break;
				}
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