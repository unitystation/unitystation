using AddressableReferences;
using Mirror;
using UnityEngine;

namespace Weapons
{
	/// <summary>
	/// Logic for toggling a weapon such as a stun baton or teleprod on or off
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	[RequireComponent(typeof(MeleeEffect))]
	public class ToggleableEffect : NetworkBehaviour, ICheckedInteractable<HandActivate>, IServerSpawn, ICheckedInteractable<InventoryApply>
	{
		private SpriteHandler spriteHandler;

		private MeleeEffect meleeEffect;

		// Sound played when turning this item on/off.
		public AddressableAudioSource ToggleSound;

		///Both used as states for the item and for the sub-catalogue in the sprite handler.
		private enum WeaponState
		{
			Off,
			On,
			NoCell
		}

		private WeaponState weaponState;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			meleeEffect = GetComponent<MeleeEffect>();
		}

		// Calls TurnOff() when item is spawned, see below.
		public void OnSpawnServer(SpawnInfo info)
		{
			TurnOff();
		}

		public void TurnOn()
		{
			meleeEffect.enabled = true;
			weaponState = WeaponState.On;
			spriteHandler.ChangeSprite((int)WeaponState.On);
		}

		public void TurnOff()
		{
			//logic to turn the teleprod off.
			meleeEffect.enabled = false;
			weaponState = WeaponState.Off;
			spriteHandler.ChangeSprite((int)WeaponState.Off);
		}

		//For making sure the user is actually conscious.
		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		#region inventoryinteraction

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
			{
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) && meleeEffect.allowScrewdriver)
				{
					return true;
				}
				else if (interaction.UsedObject != null)
				{
					if (meleeEffect.Battery == null && Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponCell))
					{
						return true;
					}
				}
			}
			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) && meleeEffect.Battery != null && meleeEffect.allowScrewdriver)
			{
				Inventory.ServerDrop(meleeEffect.batterySlot);
			}

			if (meleeEffect.Battery == null && Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponCell))
			{
				Inventory.ClientRequestTransfer(interaction.FromSlot, meleeEffect.batterySlot);
			}
		}

		#endregion

		//Activating the teleprod in-hand turns it off or off depending on its state.
		public void ServerPerformInteraction(HandActivate interaction)
		{
			SoundManager.PlayNetworkedAtPos(ToggleSound, interaction.Performer.AssumedWorldPosServer(), sourceObj: interaction.Performer);

			if (weaponState == WeaponState.Off)
			{
				if (meleeEffect.hasBattery)
				{
					if(meleeEffect.Battery != null && (meleeEffect.Battery.Watts >= meleeEffect.chargeUsage))
					{
						TurnOn();
					}
					else
					{
						Chat.AddExamineMsg(interaction.Performer, $"{gameObject.ExpensiveName()} is out of power.");
					}
				}
				else
				{
					TurnOn();
				}			
			}
			else
			{
				TurnOff();
			}
		}
	}
}
