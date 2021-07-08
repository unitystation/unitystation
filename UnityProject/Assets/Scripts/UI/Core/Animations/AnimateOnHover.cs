using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using NaughtyAttributes;

namespace UI.Core.Animations
{
	/// <summary>
	/// Changes the referenced SpriteHandler to use the given spriteSO when hovering and when not. Intended for UI.
	/// </summary>
	public class AnimateOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField, BoxGroup("References")]
		private SpriteHandler spriteHandler = default;

		[Tooltip("The sprite SO index to use when the pointer hovers over this object.")]
		[SerializeField, BoxGroup("Settings")]
		private int hoverSpriteIndex = 1;
		[Tooltip("The sprite SO variant index to use when the pointer hovers over this object. Leave as 0 for most SOs.")]
		[SerializeField, BoxGroup("Settings")]
		private int hoverSpriteVariant = 0;
		[SerializeField, BoxGroup("Settings")]
		private int noHoverSpriteIndex = 0;
		[SerializeField, BoxGroup("Settings")]
		private int noHoverSpriteVariant = 0;

		public void OnPointerEnter(PointerEventData eventData)
		{
			spriteHandler.ChangeSprite(hoverSpriteIndex, false);
			spriteHandler.ChangeSpriteVariant(hoverSpriteVariant, false);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			spriteHandler.ChangeSprite(noHoverSpriteIndex, false);
			spriteHandler.ChangeSpriteVariant(noHoverSpriteVariant, false);
		}
	}
}
