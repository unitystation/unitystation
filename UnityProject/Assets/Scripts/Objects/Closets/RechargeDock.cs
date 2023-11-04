using Systems.Construction.Parts;
using Systems.Electricity;
using UnityEngine;

namespace Objects.Closets
{
	public class RechargeDock : ClosetControl, IAPCPowerable
	{
		public float IdleWattage = 1f;

		public float ChargingWattage = 300f;


		private IChargeable ChargeableDevice;

		private APCPoweredDevice APCPoweredDevice;


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



			ChargeableDevice.ChargeBy(ChargingMultiplier * ChargingWattage);
			if (ChargeableDevice.IsFullyCharged)
			{
				SetDoor(Door.Opened);
			}

		}

		public override void CollectObjects()
		{
			foreach (var entity in registerObject.Matrix.Get<IChargeable>(registerObject.LocalPositionServer, true))
			{
				var physics = (entity as Component).GetComponent<UniversalObjectPhysics>();
				// Don't add the container to itself...
				if (physics.gameObject == gameObject) continue;

				// Can't store secured objects (exclude this check on mobs as e.g. magboots set pushable false)
				if (physics.IsNotPushable) continue;

				//No Nested ObjectContainer shenanigans
				if (physics.GetComponent<ObjectContainer>()) continue;

				objectContainer.StoreObject(physics .gameObject, physics .transform.position - transform.position);
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
}
