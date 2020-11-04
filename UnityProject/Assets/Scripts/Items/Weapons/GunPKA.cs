using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;

public class GunPKA : Gun
{
	public GameObject Projectile;

	bool allowRecharge = true;
	public float rechargeTime = 2.0f;

	public override void OnSpawnServer(SpawnInfo info)
	{
		base.OnSpawnServer(info);
		CurrentMagazine.Projectile = Projectile;
		CurrentMagazine.ServerSetAmmoRemains(1);
	}

	public override bool WillInteract(AimApply interaction, NetworkSide side)
	{
		CurrentMagazine.Projectile = Projectile;
		return base.WillInteract(interaction, side);
	}
	public override void ServerPerformInteraction(AimApply interaction)
	{
		var isSuicide = false;
		if (interaction.MouseButtonState == MouseButtonState.PRESS ||
		    (WeaponType != WeaponType.SemiAutomatic && AllowSuicide))
		{
			isSuicide = interaction.IsAimingAtSelf;
			AllowSuicide = isSuicide;
		}
		if (allowRecharge)
		{
			//enqueue the shot (will be processed in Update)
			ServerShoot(interaction.Performer, interaction.TargetVector.normalized, UIManager.DamageZone, isSuicide);
			StartCoroutine(StartCooldown());
		}
	}
	private IEnumerator StartCooldown()
	{
		allowRecharge = false;
		yield return WaitFor.Seconds(rechargeTime);
		CurrentMagazine.ServerSetAmmoRemains(1);
		CurrentMagazine.LoadProjectile(CurrentMagazine.Projectile, CurrentMagazine.ProjectilesFired);
		SoundManager.PlayNetworkedAtPos("ReloadKinetic", gameObject.AssumedWorldPosServer(), sourceObj: serverHolder);
		allowRecharge = true;
	}
}
