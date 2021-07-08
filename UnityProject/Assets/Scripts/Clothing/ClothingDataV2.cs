using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Clothing
{
	[CreateAssetMenu(fileName = "ClothingDataV2", menuName = "ScriptableObjects/ClothingData")]
	public class ClothingDataV2 : ScriptableObject
	{
		public SpriteDataSO SpriteEquipped;
		public SpriteDataSO SpriteInHandsLeft;
		public SpriteDataSO SpriteInHandsRight;
		public SpriteDataSO SpriteItemIcon;
		public Color[] Palette = new Color[8];
		public bool IsPaletted = false;

		public void Combine(ClothingDataV2 parent)
		{
			if (SpriteEquipped != null)
			{
				SpriteEquipped = parent.SpriteEquipped;
			}

			if (SpriteInHandsLeft != null)
			{
				SpriteInHandsLeft = parent.SpriteInHandsLeft;
			}

			if (SpriteInHandsRight != null)
			{
				SpriteInHandsRight = parent.SpriteInHandsRight;
			}

			if (SpriteItemIcon != null)
			{
				SpriteItemIcon = parent.SpriteItemIcon;
			}
		}

		public List<Color> GetPaletteOrNull(int variantIndex)
		{
			// TODO: Get alternate palette if necessary.
			//if (variantIndex == -1)
			//{
				return IsPaletted ? new List<Color>(Palette) : null;
			//}

			//return null;
		}
	}
}
