using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AddressableReferences;

namespace Weapons
{
	public class GunPKA : Gun
	{
		[SerializeField]
		private GameObject projectile;

		private bool allowRecharge = true;
		private float rechargeTime = 2.0f;

		public override void OnSpawnServer(SpawnInfo info)
		{
			base.OnSpawnServer(info);
			CurrentMagazine.containedBullets[0] = projectile;
			CurrentMagazine.ServerSetAmmoRemains(1);
		}

		public override bool WillInteract(AimApply interaction, NetworkSide side)
		{
			CurrentMagazine.containedBullets[0] = projectile;
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
			CurrentMagazine.LoadProjectile(projectile, 1);
			if (IsSuppressed)
			{
				if (serverHolder != null)
				{
					Chat.AddExamineMsgFromServer(serverHolder, $"The {gameObject.ExpensiveName()} silently recharges.");
				}
			}
			else
			{
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.KineticReload, gameObject.AssumedWorldPosServer(), sourceObj: serverHolder);
			}
			allowRecharge = true;
		}
	}
}