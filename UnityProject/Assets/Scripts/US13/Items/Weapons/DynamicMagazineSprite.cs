using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Core.Sprite_Handler;

public class DynamicMagazineSprite : MonoBehaviour
{
	[Serializable]
	public class MagSpritePerAmmoCount
	{
		[Tooltip("ammo count required to use this sprite in the sprite catalogue.")]
		public int AmmoCount;

		[Tooltip("Use this to pick which sprite in the SpriteHandler's SubCatalogue to use. If using an overlay and you wish to display an absence of bullets, don't leave this blank. Instead, assign generic_blanksprite in the SpriteHandler.")]
		public int CatalogueIndex;
	}

	private US13.Items.Weapons.MagazineBehaviour magazine;

	[Header("Sprite Handler to reference")]
	[Tooltip("Assign the GameObject that holds the SpriteHandler you want to use. If left null, the component will try to find a SpriteHandler in children.")]
	[SerializeField]
	private GameObject spriteHandlerObject;

	private SpriteHandler spriteHandler;

	[Header("Ammo-Sprite Catalogue Map")]
	[Tooltip("This will display the sprite listed in the SpriteHandler's subcatalogue at the index you pick here based on the highest Ammo Count number that is more than or equal to the current ammo in the magazine.")]
	[SerializeField]
	private List<MagSpritePerAmmoCount> thresholds = new List<MagSpritePerAmmoCount>();

	private void OnEnable()
	{
		ResolveReferences();
		SubscribeToMagazine();
		// set sprite correctly on spawn.
		EvaluateAndApplySprite(forceApply: true);
	}

	private void OnDisable()
	{
		UnsubscribeFromMagazine();
	}

	private void ResolveReferences()
	{
		if (magazine == null)
		{
			magazine = GetComponent<US13.Items.Weapons.MagazineBehaviour>();
		}

		// this prefers the assigned game object in the children, if any.
		if (spriteHandler == null)
		{
			if (spriteHandlerObject != null)
			{
				spriteHandler = spriteHandlerObject.GetComponent<SpriteHandler>();
			}

			// fallback to finding a child SpriteHandler if one wasn't assigned.
			if (spriteHandler == null)
			{
				spriteHandler = GetComponentInChildren<SpriteHandler>();
			}
		}
	}

	private void SubscribeToMagazine()
	{
		if (magazine == null) return;
		magazine.OnServerAmmoChanged -= OnMagazineAmmoChanged;
		magazine.OnServerAmmoChanged += OnMagazineAmmoChanged;
	}

	private void UnsubscribeFromMagazine()
	{
		if (magazine == null) return;
		magazine.OnServerAmmoChanged -= OnMagazineAmmoChanged;
	}

	private void OnMagazineAmmoChanged(int oldAmmo, int newAmmo)
	{
		EvaluateAndApplySprite();
	}

	private void EvaluateAndApplySprite(bool forceApply = false)
	{
		if (spriteHandler == null) return;

		if (magazine == null)
		{
			//  if there's no magazine ammo sprites, then clear the sprite.
			spriteHandler.PushClear();
			return;
		}

		int currentAmmo = magazine.ServerAmmoRemains;

		MagSpritePerAmmoCount chosenSprite = null;
		// ensure thresholds are evaluated such that higher AmmoCount numbers take precedence:
		foreach (var t in thresholds.OrderByDescending(t => t.AmmoCount))
		{
			if (currentAmmo >= t.AmmoCount)
			{
				chosenSprite = t;
				break;
			}
		}

		if (chosenSprite != null)
		{
			// Only apply if different catalogue page or if it has been forced.
			if (forceApply || spriteHandler.CurrentSpriteIndex != chosenSprite.CatalogueIndex)
			{
				if (chosenSprite.CatalogueIndex >= 0)
				{
					// protect against out-of-range inside SpriteHandler (it will log if invalid)
					spriteHandler.SetCatalogueIndexSprite(chosenSprite.CatalogueIndex);
				}
			}
		}
		else
		{
			// if there's no threshold matched, then clear the sprite
			if (forceApply || spriteHandler.CurrentSpriteIndex != -1)
			{
				spriteHandler.PushClear();
			}
		}
	}
}
