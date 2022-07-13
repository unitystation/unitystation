using System.Collections;
using UnityEngine;
using Communications;
using HealthV2;
using Managers;
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
				bodyPart.HealthMaster.ApplyDamageToBodyPart(gameObject, 200, AttackType.Bomb, DamageType.Burn, bodyPart.ContainedIn.BodyPartType); ///This prevents those with bomb proof armour from just tanking an explosion thats supposed to me inside them

				if (explosiveStrength > 1000)
				{
					bodyPart.HealthMaster.Gib(); //Macrobombs will gib their victim
				}
				else if (bodyPart.ContainedIn.BodyPartType == BodyPartType.Head)
				{
					bodyPart.HealthMaster.DismemberBodyPart(bodyPart.ContainedIn);
				} //Decaptiates if explosive in head
				
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

			for (int i = 0; i < TimeToDetonate; i++)
			{
				SoundManager.PlayNetworkedAtPos(beepSound, GetComponent<RegisterTile>().WorldPositionServer, sourceObj: gameObject.OrNull());
				yield return new WaitForSeconds(1); 
			}
			
			Detonate();
		}

		private void OnDisable()
		{
			Emitter = null;
		}

	}
}
