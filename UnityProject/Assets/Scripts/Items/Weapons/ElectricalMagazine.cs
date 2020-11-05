using UnityEngine;

/// <summary>
/// Gun scripts are strange, This is to make it easy to have a charging Ammo clip
/// </summary>
[RequireComponent(typeof(Battery))]
public class ElectricalMagazine : MagazineBehaviour
{

	public Battery battery;

	public int toRemove;

	public void Awake()
	{
		battery = GetComponent<Battery>();
	}

	public override void ExpendAmmo(int amount = 1)
	{
		if (toRemove > battery.Watts) 
		{
			return;
		}
		base.ExpendAmmo(amount);
		if (CustomNetworkManager.Instance._isServer == false) return;
		battery.Watts -= toRemove;
	}


	public void AddCharge()
	{
		int Ammo = Mathf.RoundToInt(magazineSize * ((float) battery.Watts / (float) battery.MaxWatts));
		Ammo = Mathf.Clamp(Ammo, 0, magazineSize);
		ServerSetAmmoRemains(Ammo);
	}
}
