using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// Holds value of prefab containing sprite we're looking for.
	/// prefab-based for now
	[RequireComponent(typeof(Image))]
	public class NetPrefabImage : NetUIStringElement
	{
		public override ElementMode InteractionMode => ElementMode.ServerWrite;
		public override string Value {
			get => prefab ?? "-1";
			protected set {
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
		private static readonly Color transparentColor = new Color(0, 0, 0, 0);

		private string prefab;

		public Image Element => element ??= GetComponent<Image>();
		private Image element;

		public override void ExecuteServer(PlayerInfo subject) { }
	}
}
