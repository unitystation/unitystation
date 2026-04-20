using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Items.Traits;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.Objects;
using US13.Systems.Construction.Parts;
using US13.Systems.Inventory;
using US13.UI.Core.ProgressBar;
using Util;

namespace US13.Items.Weapons.Melee
{
	/// <summary>
	/// Logic for toggling a weapon such as a stun baton or teleprod on or off
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	public class ToggleableEffect : NetworkBehaviour, ICheckedInteractable<HandActivate>, IServerSpawn
	{
		//On this: I'm not sure if you can assign a reference to an interface selector in another component. This is a bit dirty but it works.
		[SerializeField, Tooltip("This script will toggle the ICustomMeleeBehaviours at these indexes on the connected ItemAttributes")]
		private List<int> effectingBehaviourIDs = new List<int>();
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private ItemAttributesV2 attributes;
		[SerializeField] private InternalBattery internalBattery;


		// Sound played when turning this item on/off.
		public AddressableAudioSource ToggleSound;

		[Space(10)]
		[SerializeField]
		private WeaponState initialState = WeaponState.Off;

		///Both used as states for the item and for the sub-catalogue in the sprite handler.
		public enum WeaponState
		{
			Off,
			On,
			NoCell
		}

		private WeaponState weaponState;

		public WeaponState CurrentWeaponState
		{
			get { return weaponState; }
			set { weaponState = value; }
		}

		protected StandardProgressActionConfig ProgressConfig
			= new StandardProgressActionConfig(StandardProgressActionType.ItemTransfer);

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			if(internalBattery != null) internalBattery.OnChargeChanged += OnChargeChanged;
			SetStateFromWeaponState(initialState, true);
		}

		private void OnDestroy()
		{
			if(internalBattery != null) internalBattery.OnChargeChanged -= OnChargeChanged;
		}

		private void OnChargeChanged()
		{
			if (internalBattery.HasBatteries && (internalBattery.CurrentCharge <= 0 || weaponState == WeaponState.NoCell)) SetStateFromWeaponState(WeaponState.Off);
			else if(internalBattery.HasBatteries == false) RemoveCell(false);
		}

		// Calls TurnOff() when item is spawned, see below.
		public void OnSpawnServer(SpawnInfo info)
		{
			SetStateFromWeaponState(initialState);
		}

		private void SetStateFromWeaponState(WeaponState state, bool skipRemoval = false)
		{
			switch(state)
			{
				case WeaponState.Off:
					SetState(false);
					break;
				case WeaponState.On:
					SetState(true);
					break;
				case WeaponState.NoCell:
					RemoveCell(!skipRemoval);
					break;
			}
		}

		private void SetState(bool newState)
		{
			foreach (var i in effectingBehaviourIDs)
			{
				attributes.CustomMeleeBehaviours[i].IsEnabled = newState;
			}
			weaponState = newState ? WeaponState.On : WeaponState.Off;
			spriteHandler.SetCatalogueIndexSprite((int)weaponState);
		}

		private void RemoveCell(bool shouldRemoveBattery)
		{
			foreach (var i in effectingBehaviourIDs)
			{
				attributes.CustomMeleeBehaviours[i].IsEnabled = false;
			}
			weaponState = WeaponState.NoCell;
			spriteHandler.SetCatalogueIndexSprite((int)weaponState);
			if(shouldRemoveBattery) internalBattery.RemoveBattery();
		}

		//For making sure the user is actually conscious.
		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (ToggleSound != null)
			{
				_ = SoundManager.PlayNetworkedAtPosAsync(ToggleSound, interaction.Performer.AssumedWorldPosServer(), sourceObj: interaction.Performer);
			}

			if (weaponState == WeaponState.On)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, internalBattery.HasBatteries
						? $"You switch the {gameObject.ExpensiveName()} off"
						: $"You retract the {gameObject.ExpensiveName()}");
				SetState(false);
				return;
			}
			if (internalBattery == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You extend the {gameObject.ExpensiveName()}");
				SetState(true);
				return;
			}
			if(internalBattery.CurrentCharge > 0 && weaponState != WeaponState.NoCell)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You switch the {gameObject.ExpensiveName()} on");
				SetState(true);
				return;
			}

			string state = internalBattery.HasBatteries ? "is out of power" : "has no cell";
			Chat.AddExamineMsg(interaction.Performer, $"Your {gameObject.ExpensiveName()} {state}.");
		}
	}
}
