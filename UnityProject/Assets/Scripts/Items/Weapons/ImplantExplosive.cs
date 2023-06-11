using System.Collections;
using UnityEngine;
using Communications;
using HealthV2;
using System.Threading.Tasks;
using Mirror;

namespace Items.Weapons
{
	public class ImplantExplosive : ExplosiveBase, IServerSpawn, ICheckedInteractable<InventoryApply>, IInteractable<HandActivate>
	{
		[SerializeField] BodyPart bodyPart;

		protected override void Detonate()
		{
			if (bodyPart.ContainedIn != null)
			{
				if (explosiveStrength >= 750)
				{
					//Drop all the players stuff so that the explosion can destroy and trigger destruction events
					Inventory.ServerDropAll(bodyPart.HealthMaster.playerScript.DynamicItemStorage);

					//Macrobombs and microbombs will gib their victim
					bodyPart.HealthMaster.OnGib();

					bodyPart.ContainedIn?.OrganStorage.ServerTryRemove(gameObject);
				}
				else if (bodyPart.ContainedIn.BodyPartType != BodyPartType.Chest)
				{
					//Removes limb it is implanted in
					bodyPart.HealthMaster.DismemberBodyPart(bodyPart.ContainedIn);
				}
				else
				{
					//This prevents those with bomb proof armour from just tanking an explosion thats supposed to be inside them
					bodyPart.HealthMaster.ApplyDamageToBodyPart(gameObject, 200, AttackType.Internal, DamageType.Burn, bodyPart.ContainedIn.BodyPartType);
				}

			}

			base.Detonate();
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.UsedObject != null && interaction.UsedObject.TryGetComponent<SignalEmitter>(out var emitter) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (HackEmitter(interaction)) return;
			explosiveGUI.ServerPerformInteraction(interaction);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			explosiveGUI.ServerPerformInteraction(interaction);
		}

		[Server]
		public override IEnumerator Countdown()
		{
			if (countDownActive) yield break;

			countDownActive = true;
			spriteHandler.SetSpriteSO(activeSpriteSO);

			if (GUI != null) GUI.StartCoroutine(GUI.UpdateTimer());

			WaitForSeconds delay = new WaitForSeconds(1); //Should prevent garbage in repeated delay

			for(int i = 0; i < timeToDetonate; i++)
			{
				PlayBeepAtPos();
				yield return delay;
			}

			Detonate();
		}

		async Task PlayBeepAtPos() //async so doesn't effect how long countdown takes
		{
			Vector3 pos;

			if (bodyPart.HealthMaster != null)
			{
				pos = bodyPart.HealthMaster.playerScript.AssumedWorldPos;
			}
			else
			{
				pos = gameObject.AssumedWorldPosServer();
			}

			await SoundManager.PlayNetworkedAtPosAsync(beepSound, pos);
		}
		private void OnDisable()
		{
			Emitter = null;
		}

	}
}
