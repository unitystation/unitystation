using System.Collections;
using Mirror;
using UnityEngine;

public class Charger : NetworkBehaviour, ICheckedInteractable<HandApply>, IAPCPowered
{
	private bool IsCharging;
	private ItemSlot ChargingSlot;
	private ItemStorage itemStorage;

	private ElectricalMagazine electricalMagazine;

	public SpriteHandler spriteHandler;
	public APCPoweredDevice _APCPoweredDevice;


	public int ChargingWatts;
	private Battery battery;

	[SyncVar(hook = nameof(SyncState))]
	private ChargeState chargeState;


	public enum ChargeState
	{
		Off = 0,
		Charging,
		Charged,
		NoCharging
	}

	private void SyncState(ChargeState old, ChargeState newState)
	{
		if (old != newState)
		{
			chargeState = newState;
			spriteHandler.ChangeSprite((int)chargeState);
		}
	}

	// Start is called before the first frame update
	void Start()
	{
		SyncState(chargeState, chargeState);
	}

	private void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();
		ChargingSlot = itemStorage.GetIndexedItemSlot(0);
	}

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
			if (interaction.HandSlot.Item == null)
			{
				Inventory.ServerTransfer(ChargingSlot,interaction.HandSlot);
			}
			else
			{
				Inventory.ServerDrop(ChargingSlot);
			}

			battery = null;
			IsCharging = false;
			electricalMagazine = null;
			SyncState(chargeState,ChargeState.Off);
			_APCPoweredDevice.Resistance = 99999;
		}
		else if (ChargingSlot.Item == null)
		{
			var _object = interaction.UsedObject.GetComponent<InternalBattery>();
			if (_object == null) return;
			battery = _object.GetBattery();
			electricalMagazine = battery.GetComponent<ElectricalMagazine>();
			Inventory.ServerTransfer(interaction.HandSlot, ChargingSlot);
			if (battery != null)
			{
				_APCPoweredDevice.Resistance = battery.InternalResistance;
				SyncState(chargeState,ChargeState.Charging);
				CheckCharging();
				StartCoroutine(Recharge());
			}
		}
	}

	public void Charge()
	{
		battery.Watts += ChargingWatts;
		if (battery.MaxWatts < battery.Watts)
		{
			battery.Watts = battery.MaxWatts;
		}

		if (electricalMagazine != null)
		{
			//For electrical guns
			electricalMagazine.AddCharge();
		}

		CheckCharging();
	}

	private void CheckCharging()
	{
		if (battery.MaxWatts > battery.Watts)
		{
			IsCharging = true;
			if (ChargingWatts == 0)
			{
				SyncState(chargeState,ChargeState.NoCharging);
			}
			else
			{
				if (chargeState != ChargeState.Charging)
				{
					SyncState(chargeState,ChargeState.Charging);
				}
			}
		}
		else
		{
			SyncState(chargeState,ChargeState.Charged);
			IsCharging = false;
		}
	}

	private IEnumerator Recharge()
	{
		yield return WaitFor.Seconds(1f);
		if (IsCharging)
		{
			Charge();
			StartCoroutine(Recharge());
		}
	}

	public void PowerNetworkUpdate(float Voltage)
	{
		if (battery != null)
		{
			ChargingWatts = Mathf.RoundToInt((Voltage / battery.InternalResistance)*Voltage);
			_APCPoweredDevice.Resistance = battery.InternalResistance;
		}
	}

	public void StateUpdate(PowerStates State)
	{

	}
}