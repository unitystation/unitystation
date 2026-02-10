using System;
using Mirror;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Items.Traits;
using US13.Items.Weapons.ActivatableWeaponComponents.Client;
using US13.Items.Weapons.ActivatableWeaponComponents.Server;
using Random = UnityEngine.Random;

namespace US13.Items.Weapons.Melee
{
	public class EnergySword : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
	{
		[SerializeField]
		[SyncVar(hook = nameof(SyncState))] private SwordColor color = default;

		private ActivatableWeapon av;
		private EmitLightOnActivate avEmitLight;
		private ChangeSpriteOnActivate avChangeSprite;

		[SerializeField] private EswordSprites Sprites = default;

		private void Awake()
		{
			av = GetComponent<ActivatableWeapon>();
			avEmitLight = GetComponent<EmitLightOnActivate>();
			avChangeSprite = GetComponent<ChangeSpriteOnActivate>();
		}

		private void Start()
		{
			if (color == SwordColor.Random)
			{
				// Get random color
				SyncState(color, (SwordColor)Enum.GetValues(typeof(SwordColor)).GetValue(Random.Range(1, 5)));
			}
		}

		public void SyncState(SwordColor oldState, SwordColor newState)
		{
			color = newState;
			UpdateCol();
		}

		private void UpdateCol()
		{
			avEmitLight.Color = GetLightSourceColor(color);
			avChangeSprite.ActivatedSprites = GetItemSprites(color);
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.TargetObject != gameObject) return false;
			//only works if screwdriver is in hand
			if (!interaction.IsFromHandSlot) return false;

			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
			       Validations.HasItemTrait(interaction, CommonTraits.Instance.Multitool);
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			AdjustColor(interaction.UsedObject, interaction.Performer);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
			       Validations.HasItemTrait(interaction, CommonTraits.Instance.Multitool);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			AdjustColor(interaction.UsedObject, interaction.Performer);
		}

		private void AdjustColor(GameObject usedObject, GameObject performer)
		{
			if (av.IsActive)
			{
				Chat.AddExamineMsgFromServer(performer, "You can't adjust the sword while it's <i>on</i>!");
				return;
			}

			if (color == SwordColor.Rainbow)
			{
				Chat.AddExamineMsgFromServer(performer, "It's <i>already</i> fabulous!");
				return;
			}

			if (Validations.HasItemTrait(usedObject, CommonTraits.Instance.Screwdriver))
			{
				var num = color;
				if (num + 1 > SwordColor.Purple)
				{
					SyncState(color, SwordColor.Red);
				}
				else
				{
					SyncState(color, num + 1);
				}
				Chat.AddExamineMsgFromServer(performer, "You adjust the crystalline beam emitter.");
			}
			else if (Validations.HasItemTrait(usedObject, CommonTraits.Instance.Multitool))
			{
				SyncState(color, SwordColor.Rainbow);
				Chat.AddExamineMsgFromServer(performer,
					"You tinker with the sword's firmware using the multitool.\nIt reports; <b>RNBW_ENGAGE</b>.");
			}
		}

		private ItemsSprites GetItemSprites(SwordColor swordColor)
		{
			switch (swordColor)
			{
				case SwordColor.Red:
					return Sprites.Red;
				case SwordColor.Blue:
					return Sprites.Blue;
				case SwordColor.Green:
					return Sprites.Green;
				case SwordColor.Purple:
					return Sprites.Purple;
				case SwordColor.Rainbow:
					return Sprites.Rainbow;
			}

			return Sprites.Red;
		}

		private Color GetLightSourceColor(SwordColor swordColor)
		{
			switch (swordColor)
			{
				case SwordColor.Red:
					return new Color32(250, 130, 130, 169); // LIGHT_COLOR_RED
				case SwordColor.Blue:
					return new Color32(64, 206, 255, 169); // LIGHT_COLOR_LIGHT_CYAN
				case SwordColor.Green:
					return new Color32(100, 200, 100, 169); // LIGHT_COLOR_GREEN
				case SwordColor.Purple:
					return new Color32(155, 81, 255, 169); // LIGHT_COLOR_LAVENDER
			}

			return default;
		}

		public enum SwordColor
		{
			Random = 0,
			Red = 1,
			Blue = 2,
			Green = 3,
			Purple = 4,
			Rainbow = 5
		}
	}

	[Serializable]
	public class EswordSprites{
		public ItemsSprites Blue = new ItemsSprites();
		public ItemsSprites Green = new ItemsSprites();
		public ItemsSprites Purple = new ItemsSprites();
		public ItemsSprites Rainbow = new ItemsSprites();
		public ItemsSprites Red = new ItemsSprites();
	}
}