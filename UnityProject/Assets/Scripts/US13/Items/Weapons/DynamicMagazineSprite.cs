using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Core.Sprite_Handler;

namespace US13.Items.Weapons
{
	[Serializable]
	public class MagSpritePerAmmoCount
	{
		[Tooltip("ammo count required to use this sprite in the sprite catalogue.")]
		public int AmmoCount;

		[Tooltip("Use this to pick which sprite in the SpriteHandler's SubCatalogue to use. If using an overlay and you wish to display an absence of bullets, don't leave this blank. Instead, assign generic_blanksprite in the SpriteHandler.")]
		public int CatalogueIndex;
	}

	[RequireComponent(typeof(MagazineBehaviour))]
	public class DynamicMagazineSprite : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("Reference to the magazine behaviour on this prefab")]
		private MagazineBehaviour magazine;
		public MagazineBehaviour Magazine => magazine;

		[Header("Sprite Handler to reference")]
		[Tooltip("Assign the SpriteHandler component that we will be using to push the different sprites to.")]
		[SerializeField]
		private SpriteHandler spriteHandler;
		public SpriteHandler SpriteHandler => spriteHandler;

		[Header("Ammo-Sprite Catalogue Map")]
		[Tooltip("This will display the sprite listed in the SpriteHandler's subcatalogue at the index you pick here based on the highest Ammo Count number that is more than or equal to the current ammo in the magazine.")]
		[SerializeField]
		private List<MagSpritePerAmmoCount> thresholds = new();

		private void OnValidate()
		{
			if (spriteHandler == null) Debug.LogError($"{nameof(spriteHandler)} is not assigned on {gameObject.name}", this);
		}

		private void OnEnable()
		{
			thresholds.Sort((a, b) => b.AmmoCount.CompareTo(a.AmmoCount));
			magazine.OnServerAmmoChanged += OnMagazineAmmoChanged;
			EvaluateAndApplySprite(forceApply: true);
		}

		private void OnDisable()
		{
			magazine.OnServerAmmoChanged -= OnMagazineAmmoChanged;
		}

		private void OnMagazineAmmoChanged(int oldAmmo, int newAmmo)
		{
			EvaluateAndApplySprite();
		}

		private void EvaluateAndApplySprite(bool forceApply = false)
		{
			// thresholds are pre-sorted descending in OnEnable, so first match is the highest qualifying threshold.
			int currentAmmo = magazine.ServerAmmoRemains;
			MagSpritePerAmmoCount chosenSprite = null;
			foreach (var t in thresholds)
			{
				if (currentAmmo >= t.AmmoCount)
				{
					chosenSprite = t;
					break;
				}
			}

			if (chosenSprite == null)
			{
				if (forceApply || spriteHandler.CurrentSpriteIndex != -1)
				{
					spriteHandler.PushClear();
				}
				return;
			}

			if (forceApply || spriteHandler.CurrentSpriteIndex != chosenSprite.CatalogueIndex)
			{
				spriteHandler.SetCatalogueIndexSprite(chosenSprite.CatalogueIndex);
			}
		}
	}
}
