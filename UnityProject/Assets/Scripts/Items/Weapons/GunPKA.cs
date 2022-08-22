using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AddressableReferences;
using HealthV2;
using Messages.Server.SoundMessages;

namespace Weapons
{
	public class GunPKA : Gun
	{
		[SerializeField]
		private GameObject projectile;

		private bool allowRecharge = true;

		[SerializeField]
		private float rechargeTime = 2.0f;


		public AddressableAudioSource rechargeSound;

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
				SoundManager.PlayNetworkedAtPos(rechargeSound, gameObject.AssumedWorldPosServer(), sourceObj: serverHolder);
			}
			allowRecharge = true;
		}

		protected override IEnumerator SuicideAction(GameObject performer)
		{
			var playerHealth = performer.GetComponent<PlayerHealthV2>();
			foreach (var bodyPart in playerHealth.SurfaceBodyParts)
			{
				if(bodyPart.BodyPartType != BodyPartType.Head) continue;
				playerHealth.DismemberBodyPart(bodyPart);
				break;
			}
			playerHealth.Death(); //Just incase
			string suicideMessage = $"{playerHealth.playerScript.visibleName} puts the gun to his mouth before blowing off his head completely.";
			Chat.AddActionMsgToChat(performer, suicideMessage, suicideMessage);
			yield return null;
		}
	}
}