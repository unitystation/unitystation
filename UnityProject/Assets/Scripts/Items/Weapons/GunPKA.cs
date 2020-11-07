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
		if (allowRecharge)
		{
			//enqueue the shot (will be processed in Update)
			base.ServerPerformInteraction(interaction);
			StartCoroutine(StartCooldown());
		}
	}
	private IEnumerator StartCooldown()
	{
		allowRecharge = false;
		yield return WaitFor.Seconds(rechargeTime);
		CurrentMagazine.ServerSetAmmoRemains(1);
		CurrentMagazine.LoadProjectile(Projectile, 1);
		if (isSuppressed)
		{
			Chat.AddExamineMsgFromServer(serverHolder, $"Your {gameObject.ExpensiveName()} silently recharges");
		}
		else
		{
			SoundManager.PlayNetworkedAtPos("ReloadKinetic", gameObject.AssumedWorldPosServer(), sourceObj: serverHolder);
		}
		allowRecharge = true;
	}
}
