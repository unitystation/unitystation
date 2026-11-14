using System.Collections.Generic;
using UnityEngine;
using US13.Actions;
using US13.Clothing;
using US13.HealthV2.Living;

namespace US13.Player
{
	[System.Serializable]
	public class BodyPartControllableLight : BodyPartBehavior
	{

		[UnityEngine.SerializeReference] private ItemLightControl ItemLightControl;
		[UnityEngine.SerializeReference] private ItemActionButton actionButton;

		[SerializeField] private SpriteDataSO baseEyeSprite;
		[SerializeField] private SpriteDataSO litEyeSprite;


		public override void OnInitialize()
		{
			ItemLightControl = GetBodyPartBehavior<ItemLightControl>();
			actionButton = GetBodyPartBehavior<ItemActionButton>();
		}

		public List<ColourAndSize> IntensityColours = new List<ColourAndSize>();

		[System.Serializable]
		public struct ColourAndSize
		{
			public Color Color;
			public int Size;
		}

		public int Index = 0;

		public override void OnBehaviorEnabled()
		{
			if (actionButton != null)
			{
				actionButton.ServerActionClicked += ToggleLight;
			}
		}

		public override void OnBehaviorDisabled()
		{
			if (actionButton != null)
			{
				actionButton.ServerActionClicked -= ToggleLight;
			}
		}


		private void ToggleLight()
		{
			Index++;
			var ColourIndex = Index - 1;

			if (ColourIndex < IntensityColours.Count)
			{
				ItemLightControl.Toggle(true);
				ItemLightControl.SetColor(IntensityColours[ColourIndex].Color);
				ItemLightControl.SetSize(IntensityColours[ColourIndex].Size);
				RelatedPart.SpritePrefab.UpdateSpritesForImplant(RelatedPart, ClothingHideFlags.HIDE_NONE, litEyeSprite, RelatedPart.SpritePrefab.SpriteOrder);

			}
			else
			{
				Index = 0;
				ItemLightControl.Toggle(false);
				RelatedPart.SpritePrefab.UpdateSpritesForImplant(RelatedPart, ClothingHideFlags.HIDE_NONE, baseEyeSprite, RelatedPart.SpritePrefab.SpriteOrder);
			}
		}

	}
}
