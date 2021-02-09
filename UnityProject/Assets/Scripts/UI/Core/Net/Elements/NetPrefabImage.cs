using UnityEngine;
using UnityEngine.UI;

/// Holds value of prefab containing sprite we're looking for.
/// prefab-based for now
[RequireComponent(typeof(Image))]
public class NetPrefabImage : NetUIStringElement
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;
	public override string Value {
		get { return prefab ?? "-1"; }
		set {
			externalChange = true;
			Sprite sprite = null;
			if (!string.IsNullOrEmpty(value) && !value.Equals("-1"))
			{
				sprite = Spawn.GetPrefabByName(value)?.GetComponentInChildren<SpriteRenderer>()?.sprite;
			}

			Element.sprite = sprite;
			Element.color = sprite == null ? transparentColor : Color.white;
			prefab = value;
			externalChange = false;
		}
	}
	private static readonly Color transparentColor = new Color(0,0,0,0);
	private Image element;
	private string prefab;

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