using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;
using AddressableReferences;

public class GunPKA : Gun
{
	public GameObject Projectile;

	[SerializeField] private AddressableAudioSource ReloadKinetic = null;

	bool allowRecharge = true;
	public float rechargeTime = 2.0f;

	public override void OnSpawnServer(SpawnInfo info)
	{
		base.OnSpawnServer(info);
		CurrentMagazine.containedBullets[0] = Projectile;
		CurrentMagazine.ServerSetAmmoRemains(1);
	}

	public override bool WillInteract(AimApply interaction, NetworkSide side)
	{
		CurrentMagazine.containedBullets[0] = Projectile;
		return base.WillInteract(interaction, side);
	}

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
		if (IsSuppressed)
		{
			if (serverHolder != null)
			{
				Chat.AddExamineMsgFromServer(serverHolder, $"The {gameObject.ExpensiveName()} silently recharges");
			}
		}
		else
		{
			SoundManager.PlayNetworkedAtPos(ReloadKinetic, gameObject.AssumedWorldPosServer(), sourceObj: serverHolder);
		}
		allowRecharge = true;
	}
}
