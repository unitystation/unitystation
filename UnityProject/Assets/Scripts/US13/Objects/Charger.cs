using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Items.Traits;
using US13.Items.Weapons;
using US13.Managers.UpdateManager;
using US13.Objects.Engineering;
using US13.Systems.Construction.Parts;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Inventory;

namespace US13.Objects
{
	public class Charger : MonoBehaviour, ICheckedInteractable<HandApply>, IAPCPowerable, IExaminable
	{
		public ItemStorage itemStorage;
		private ItemSlot ChargingSlot;

		private ElectricalMagazine electricalMagazine;

		[SerializeField]
		private APCPoweredDevice _APCPoweredDevice = default;
		private SpriteHandler spriteHandler;

		private int ChargingWatts;
		private Battery battery;

		#region Lifecycle

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			ChargingSlot = itemStorage.GetIndexedItemSlot(0);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		#endregion Lifecycle

		public enum SpriteState
		{
			Idle = 0,
			Charging = 1,
			Charged = 2,
			Error = 3,
			Off = 4,
			Open = 5
		}

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject != null)
			{
				if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.InternalBattery)) return false;
			}

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (ChargingSlot.Item && interaction.UsedObject == null)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);

				if (interaction.HandSlot.Item == null)
				{
					Inventory.ServerTransfer(ChargingSlot, interaction.HandSlot);
				}
				else
				{
					Inventory.ServerDrop(ChargingSlot);
				}

				battery = null;
				electricalMagazine = null;
				SetSprite(SpriteState.Idle);
				_APCPoweredDevice.Resistance = 99999;
			}
			else if (ChargingSlot.Item == null && interaction.UsedObject != null && Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.InternalBattery))
			{
				var _object = interaction.UsedObject.GetComponent<InternalBattery>();
				if (_object == null) return;
				battery = _object.GetBattery();
				electricalMagazine = battery.GetComponent<ElectricalMagazine>();
				Inventory.ServerTransfer(interaction.HandSlot, ChargingSlot);
				if (battery != null)
				{
					_APCPoweredDevice.Resistance = battery.InternalResistance;
					UpdateManager.Add(UpdateMe, 1);
					UpdateMe();
				}
			}
		}

		#endregion Interaction

		private void UpdateMe()
		{
			CheckCharging();
		}

		private void CheckCharging()
		{
			if (battery.Watts < battery.MaxWatts)
			{
				if (ChargingWatts == 0)
				{
					SetSprite(SpriteState.Error);
					return;
				}

				SetSprite(SpriteState.Charging);
				AddCharge();
			}
			else
			{
				SetSprite(SpriteState.Charged);
			}
		}

		private void AddCharge()
		{
			battery.Watts += ChargingWatts;

			if (battery.Watts > battery.MaxWatts)
			{
				battery.Watts = battery.MaxWatts;
			}

			if (electricalMagazine != null)
			{
				//For electrical guns
				electricalMagazine.AddCharge();
				var GunElectrical = ChargingSlot.Item.GetComponent<GunElectrical>();
				if (GunElectrical != null)
				{
					GunElectrical.UpdateChargeSprite();
				}
			}
		}

		private void SetSprite(SpriteState newState)
		{
			spriteHandler.SetCatalogueIndexSprite((int)newState);
		}

		public void PowerNetworkUpdate(float voltage)
		{
			if (battery != null)
			{
				ChargingWatts = Mathf.RoundToInt((voltage / battery.InternalResistance) * voltage);
				_APCPoweredDevice.Resistance = battery.InternalResistance;
			}
		}

		public void StateUpdate(PowerState state) { }


		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (battery == null)
			{
				return "The display on the charges state That there is no battery connected";
			}
			else
			{

				return $"The display on the charges state battery is at {100 * ((float)battery.Watts / (float)battery.MaxWatts)} and charging at {ChargingWatts}W";
			}

		}
	}
}
