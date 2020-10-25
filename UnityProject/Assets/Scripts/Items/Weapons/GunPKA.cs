using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;

public class GunPKA : Gun
{
	public GameObject Projectile;

	bool allowRecharge = true;
	public float rechargeTime = 2.0f;
	public override void ServerPerformInteraction(AimApply interaction)
	{
		base.ServerPerformInteraction(interaction);
		if (allowRecharge)
		{
			StartCoroutine(StartCooldown());
		}
	}
	private IEnumerator StartCooldown()
	{
		allowRecharge = false;
		yield return WaitFor.Seconds(rechargeTime);
		CurrentMagazine.ServerSetAmmoRemains(1);
		CurrentMagazine.LoadProjectile(Projectile, CurrentMagazine.ProjectilesFired);
		SoundManager.PlayNetworkedAtPos("ReloadKinetic", gameObject.AssumedWorldPosServer(), sourceObj: serverHolder);
		allowRecharge = true;
	}
}
