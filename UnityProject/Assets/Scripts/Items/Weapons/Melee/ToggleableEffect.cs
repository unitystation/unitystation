using AddressableReferences;
using Mirror;
using Systems.Construction.Parts;
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
		[SerializeField] private bool toggleAffectsComponent = false;

		private SpriteHandler spriteHandler;

		private MeleeEffect meleeEffect;

		// Sound played when turning this item on/off.
		public AddressableAudioSource ToggleSound;

		[Space(10)]
		[SerializeField]
		private WeaponState intialState = WeaponState.Off;

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

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			meleeEffect = GetComponent<MeleeEffect>();
			weaponState = intialState;
		}

		// Calls TurnOff() when item is spawned, see below.
		public void OnSpawnServer(SpawnInfo info)
		{
			switch(weaponState)
			{
				case WeaponState.Off:
					TurnOff();
					break;
				case WeaponState.On:
					TurnOn();
					break;
				case WeaponState.NoCell:
					RemoveCell();
					break;
			}
		}

		public void TurnOn()
		{
			if(toggleAffectsComponent) meleeEffect.enabled = true;
			weaponState = WeaponState.On;
			spriteHandler.ChangeSprite((int)WeaponState.On);
		}

		public void TurnOff()
		{
			//logic to turn the teleprod off.
			if (toggleAffectsComponent) meleeEffect.enabled = false;
			weaponState = WeaponState.Off;
			spriteHandler.ChangeSprite((int)WeaponState.Off);
		}

		private void RemoveCell()
		{
			//Logic for removing the items battery
			if (toggleAffectsComponent) meleeEffect.enabled = false;
			weaponState = WeaponState.NoCell;
			spriteHandler.ChangeSprite((int)WeaponState.NoCell);
			Inventory.ServerDrop(meleeEffect.batterySlot);
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
				RemoveCell();
			}

			if (meleeEffect.Battery == null && Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponCell))
			{
				if (interaction.UsedObject.GetComponent<Battery>().MaxWatts >= meleeEffect.chargeUsage)
				{
					Inventory.ServerTransfer(interaction.FromSlot, meleeEffect.batterySlot);
					TurnOff();
				}
				else
				{
					Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} requires a higher capacity cell.");
				}
			}
		}

		#endregion

		//Activating the teleprod in-hand turns it off or off depending on its state.
		public void ServerPerformInteraction(HandActivate interaction)
		{
			SoundManager.PlayNetworkedAtPos(ToggleSound, interaction.Performer.AssumedWorldPosServer(), sourceObj: interaction.Performer);

			if (weaponState == WeaponState.Off || weaponState == WeaponState.NoCell)
			{
				if (meleeEffect.hasBattery)
				{
					if(meleeEffect.Battery != null && (meleeEffect.Battery.Watts >= meleeEffect.chargeUsage) && weaponState != WeaponState.NoCell)
					{
						TurnOn();
					}
					else
					{
						Chat.AddExamineMsg(interaction.Performer, $"{gameObject.ExpensiveName()} is out of power or has no cell.");
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
