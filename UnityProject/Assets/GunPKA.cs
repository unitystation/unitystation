using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunPKA : Gun
{

	bool allowRecharge = true;
	public override void ServerPerformInteraction(AimApply interaction)
	{
		//do we need to check if this is a suicide (want to avoid the check because it involves a raycast).
		//case 1 - we are beginning a new shot, need to see if we are shooting ourselves
		//case 2 - we are firing an automatic and are currently shooting ourselves, need to see if we moused off
		//	ourselves.

		//enqueue the shot (will be processed in Update)
		ServerShoot(interaction.Performer, interaction.TargetVector.normalized, UIManager.DamageZone, false);
		if (ammoType == AmmoType.Internal && allowRecharge)
		{
			Chat.AddGameWideSystemMsgToChat("Start recharge.");
			StartCoroutine(StartCooldown());
		}
	}
	private IEnumerator StartCooldown()
	{
		allowRecharge = false;
		yield return WaitFor.Seconds(3);
		CurrentMagazine.ExpendAmmo(-1);
		Chat.AddGameWideSystemMsgToChat("Reloaded?");
		SoundManager.PlayNetworkedAtPos("ReloadKinetic", gameObject.AssumedWorldPosServer());
		allowRecharge = true;
	}
}
