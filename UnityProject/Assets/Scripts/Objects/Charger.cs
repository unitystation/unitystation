using System;
using UnityEngine;

public class Charger : MonoBehaviour, ICheckedInteractable<HandApply>, IAPCPowered
{
	private ItemStorage itemStorage;
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
		itemStorage = GetComponent<ItemStorage>();
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
		if (!DefaultWillInteract.Default(interaction, side)) return false;
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
				Inventory.ServerTransfer(ChargingSlot,interaction.HandSlot);
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
		else if (ChargingSlot.Item == null && interaction.UsedObject != null)
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

		if (electricalMagazine != null)
		{
			//For electrical guns
			electricalMagazine.AddCharge();
		}
	}

	private void SetSprite(SpriteState newState)
	{
		spriteHandler.ChangeSprite((int) newState);
	}

	public void PowerNetworkUpdate(float Voltage)
	{
		if (battery != null)
		{
			ChargingWatts = Mathf.RoundToInt((Voltage / battery.InternalResistance) * Voltage);
			_APCPoweredDevice.Resistance = battery.InternalResistance;
		}
	}

	public void StateUpdate(PowerStates State)
	{

	}
}
