using UnityEngine;

/// <summary>
/// Gun scripts are strange, This is to make it easy to have a charging Ammo clip
/// </summary>
public class ElectricalMagazine : MagazineBehaviour
{
	public Battery battery;

	public override void ExpendAmmo(int amount = 1)
	{
		base.ExpendAmmo(amount);
		if (CustomNetworkManager.Instance._isServer == false) return;
		if (ServerAmmoRemains == 0)
		{
			battery.Watts = 0;
		}
		else
		{
			battery.Watts = Mathf.RoundToInt( battery.MaxWatts * ((float)ServerAmmoRemains /  (float) magazineSize));
		}
	}


	public void AddCharge()
	{
		int Ammo = Mathf.RoundToInt(magazineSize * ((float) battery.Watts / (float) battery.MaxWatts));
		Ammo = Mathf.Clamp(Ammo, 0, magazineSize);
		ServerSetAmmoRemains(Ammo);
	}
}
