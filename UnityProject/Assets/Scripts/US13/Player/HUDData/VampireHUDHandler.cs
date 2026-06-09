using UnityEngine;
using US13.Core.Sprite_Handler;
using Util;

namespace US13.Player.HUDData
{
	public class VampireHUDHandler : MonoBehaviour
	{

		[SerializeField] private SpriteHandler iconSymbol;

		public void SetVisible(bool visible)
		{
			iconSymbol.SetActive(visible);
		}

		public void UpdateStage(int vampirismStage)
		{
			int variantIndex = Mathf.Clamp(vampirismStage, 0, 3);
			iconSymbol.SetSpriteVariant(variantIndex, false);
		}

	}
}


