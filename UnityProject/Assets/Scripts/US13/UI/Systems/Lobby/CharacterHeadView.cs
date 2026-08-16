using UnityEngine;
using UnityEngine.UI;
using US13.Player;

namespace US13.UI.Systems.Lobby
{
	/// <summary>
	/// Shows the active character's head as a stack of UI images.
	/// </summary>
	public class CharacterHeadView : MonoBehaviour
	{
		[SerializeField]
		private Image placeholderImage = default;

		[SerializeField]
		private RectTransform layerContainer = default;

		private void OnEnable()
		{
			PlayerManager.CharacterManager.OnActiveCharacterChanged += Rebuild;
			Rebuild();
		}

		private void OnDisable()
		{
			PlayerManager.CharacterManager.OnActiveCharacterChanged -= Rebuild;
		}

		private void Rebuild()
		{
			if (layerContainer == null) return;

			ClearLayers();

			if (PlayerManager.CharacterManager.Characters.Count == 0)
			{
				ShowPlaceholder(true);
				return;
			}

			var layers = CharacterHeadResolver.Resolve(PlayerManager.ActiveCharacter);
			if (layers.Count == 0)
			{
				ShowPlaceholder(true);
				return;
			}

			foreach (var layer in layers)
			{
				CreateLayer(layer);
			}

			ShowPlaceholder(false);
		}

		private void ClearLayers()
		{
			for (int i = layerContainer.childCount - 1; i >= 0; i--)
			{
				Destroy(layerContainer.GetChild(i).gameObject);
			}
		}

		private void CreateLayer(HeadLayer layer)
		{
			var layerObject = new GameObject("HeadLayer", typeof(RectTransform), typeof(Image));
			var rect = layerObject.GetComponent<RectTransform>();
			rect.SetParent(layerContainer, false);
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;

			var image = layerObject.GetComponent<Image>();
			image.sprite = layer.Sprite;
			image.color = layer.Colour;
			image.raycastTarget = false;
		}

		private void ShowPlaceholder(bool show)
		{
			if (placeholderImage == null) return;

			placeholderImage.enabled = show;
		}
	}
}
