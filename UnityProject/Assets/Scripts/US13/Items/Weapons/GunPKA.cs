using System.Collections;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Managers;
using Util;

namespace US13.Items.Weapons
{
	public class GunPKA : Gun
	{
		[SerializeField]
		private GameObject projectile;

		public bool allowRecharge = true;

		[SerializeField]
		private float rechargeTime = 2.0f;

		[SerializeField]
		public SpriteHandler rechargeSpriteHandler;

		public AddressableAudioSource rechargeSound;

		public int MaxRecharges = 1;

		public Coroutine Charging = null;

		public override void OnSpawnServer(SpawnInfo info)
		{
			base.OnSpawnServer(info);
			CurrentMagazine.containedBullets[0] = projectile;
			CurrentMagazine.ServerSetAmmoRemains(MaxRecharges);
			if (rechargeSpriteHandler != null)
			{
				rechargeSpriteHandler.PushClear();
			}
		}

		public override bool WillInteract(AimApply interaction, NetworkSide side)
		{
			CurrentMagazine.containedBullets[0] = projectile;
			return base.WillInteract(interaction, side);
		}

		public override void ServerPerformInteraction(AimApply interaction)
		{
			if (CurrentMagazine.ServerAmmoRemains > 0)
			{
				//enqueue the shot (will be processed in Update)
				base.ServerPerformInteraction(interaction);
				if (Charging == null && allowRecharge)
				{
					Charging = StartCoroutine(StartCooldown());
				}

			}
		}

		private IEnumerator StartCooldown()
		{
			allowRecharge = false;

			if (rechargeSpriteHandler != null)
			{
				rechargeSpriteHandler.PushTexture();
			}

			while (MaxRecharges > CurrentMagazine.ServerAmmoRemains )
			{
				yield return WaitFor.Seconds(rechargeTime);
				CurrentMagazine.ServerSetAmmoRemains(CurrentMagazine.ServerAmmoRemains + 1);
				CurrentMagazine.LoadProjectile(projectile, 1);
			}

			Charging = null;

			if (IsSuppressed)
			{
				if (ServerHolder != null)
				{
					Chat.AddExamineMsgFromServer(ServerHolder, $"The {gameObject.ExpensiveName()} silently recharges.");
				}
			}
			else
			{
				SoundManager.PlayNetworkedAtPos(rechargeSound, gameObject.AssumedWorldPosServer(), sourceObj: ServerHolder);
			}

			if (rechargeSpriteHandler != null)
			{
				rechargeSpriteHandler.PushClear();
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