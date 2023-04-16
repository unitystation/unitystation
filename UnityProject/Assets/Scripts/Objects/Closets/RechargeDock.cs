using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Objects;
using Systems.Electricity;
using UnityEngine;

public class RechargeDock : ClosetControl, IAPCPowerable
{
	public float IdleWattage = 1f;

	public float ChargingWattage = 300f;


	public IChargeable ChargeableDevice;

	public APCPoweredDevice APCPoweredDevice;


	public bool On = false;

	public override void Awake()
	{
		base.Awake();
		APCPoweredDevice = this.GetComponentCustom<APCPoweredDevice>();
		APCPoweredDevice.Wattusage = IdleWattage;
		SetDoor(Door.Opened);
	}

	public void PowerNetworkUpdate(float voltage){}

	public void StateUpdate(PowerState state) { }

	public void ChargeUpdate()
	{

		float ChargingMultiplier = 1;

		switch (APCPoweredDevice.State)
		{
			case PowerState.Off:
				ChargingMultiplier = 0;
				break;
			case PowerState.LowVoltage:
				ChargingMultiplier = 0.5f;
				break;
			case PowerState.On:
				ChargingMultiplier = 1;
				break;
			case PowerState.OverVoltage:
				ChargingMultiplier = 2;
				break;
		}

		if (ChargeableDevice.FullyCharged())
		{
			SetDoor(Door.Opened);
		}
		else
		{
			ChargeableDevice.ChargeBy(ChargingMultiplier * ChargingWattage);
		}
	}

	public override void CollectObjects()
	{
		foreach (var entity in registerObject.Matrix.Get<IChargeable>(registerObject.LocalPositionServer, true))
		{
			var Universal = (entity as Component).GetComponent<UniversalObjectPhysics>();
			// Don't add the container to itself...
			if (Universal.gameObject == gameObject) continue;

			// Can't store secured objects (exclude this check on mobs as e.g. magboots set pushable false)
			if (Universal.IsNotPushable) continue;

			//No Nested ObjectContainer shenanigans
			if (Universal.GetComponent<ObjectContainer>()) continue;

			objectContainer.StoreObject(Universal.gameObject, Universal.transform.position - transform.position);
			ChargeableDevice = entity;
			On = true;
			APCPoweredDevice.Wattusage = ChargingWattage;
			UpdateManager.Add(ChargeUpdate, 1);
			return;
		}
	}

	public override void ReleaseObjects()
	{
		objectContainer.RetrieveObjects();
		ChargeableDevice = null;
		On = false;
		APCPoweredDevice.Wattusage = IdleWattage;
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ChargeUpdate);
	}
}
