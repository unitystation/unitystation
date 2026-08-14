using UnityEngine;
using US13.HealthV2;
using US13.Items.Weapons;
using US13.Managers;
using US13.Objects;
using US13.Objects.Doors;
using US13.Systems.Construction.Parts;
using US13.Systems.Inventory;
using US13.Systems.Spells;
using Util;

public class Charge : Spell
{
	public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition, BodyPartType targetZone)
	{
		var Slot = caster.Script.GetComponent<DynamicItemStorage>().GetActiveHandSlot();

		if (Slot == null) return false;

		if (Slot.Item.TryGetComponent<GunPKA>(out var GunPKA))
		{
			var magazine = GunPKA.CurrentMagazine;
			var remainingToLoad = GunPKA.MaxRecharges - magazine.ServerAmmoRemains;

			for (int i = 0; i < remainingToLoad; i++)
			{
				magazine.LoadProjectile(GunPKA.Projectile, 1);
			}

			GunPKA.CurrentMagazine.ServerSetAmmoRemains(GunPKA.MaxRecharges);
			return true;
		}

		if (Slot.Item.TryGetComponent<InternalBattery>(out var newBattery) )
		{

			var batteryToCharge = newBattery;
			var electricalMagazine = newBattery.GetComponent<GunElectrical>()?.CurrentElectricalMag;
			batteryToCharge.CurrentCharge = batteryToCharge.MaxCharge;
			if (electricalMagazine != null)
			{
				//For electrical guns
				electricalMagazine.AddCharge();
				var GunElectrical = Slot.Item.GetComponent<GunElectrical>();
				if (GunElectrical != null)
				{
					GunElectrical.UpdateChargeSprite();
				}
			}
			return true;

		}

		if (Slot.Item.TryGetComponent<IChargeable>(out var Chargeable))
		{
			Chargeable.ChargeBy(99999999999999999999f);
			return true;

		}
		if (Slot.Item.TryGetComponent<Battery>(out var Battery) )
		{
			Battery.Watts = Battery.MaxWatts;
			return true;

		}
		return false;
	}
}
