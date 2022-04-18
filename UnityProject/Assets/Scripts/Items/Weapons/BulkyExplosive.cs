using System.Collections;
using AddressableReferences;
using Communications;
using Mirror;
using UnityEngine;

namespace Items.Weapons
{
	/// <summary>
	/// Interaction script for Bulky explosives that cannot be picked up and instead pulled.
	/// </summary>
	public class BulkyExplosive : ExplosiveBase, ICheckedInteractable<HandApply>
	{
		[SerializeField] private ItemTrait wrenchTrait;
		private float currentCountdown;

		private void OnDisable()
		{
			StopCoroutine(Countdown());
			StopCoroutine(BeepBeep());
		}

		[Server]
		public override IEnumerator Countdown()
		{
			countDownActive = true;
			spriteHandler.SetSpriteSO(activeSpriteSO);
			if (GUI != null) GUI.StartCoroutine(GUI.UpdateTimer());
			StartCoroutine(BeepBeep());
			currentCountdown = timeToDetonate;
			while (currentCountdown > 0)
			{
				currentCountdown -= 1f;
				yield return WaitFor.Seconds(1f);
			}
			countDownActive = false;
			Detonate();
		}

		private IEnumerator BeepBeep()
		{
			while (countDownActive)
			{
				SoundManager.PlayNetworkedAtPos(beepSound, gameObject.AssumedWorldPosServer());
				yield return WaitFor.Seconds(currentCountdown > timeToDetonate / 2 ? 2f : 0.5f);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (explosiveType != ExplosiveType.SyndicateBomb ||
			    DefaultWillInteract.Default(interaction, side) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(HackEmitter(interaction)) return;
			if (interaction.HandObject != null && interaction.HandObject.Item().HasTrait(wrenchTrait))
			{
				objectBehaviour.SetIsNotPushable(!objectBehaviour.IsNotPushable);
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Wrench, gameObject.AssumedWorldPosServer());
				var wrenchText = objectBehaviour.IsNotPushable ? "unwrench" :  "wrench down";
				Chat.AddExamineMsg(interaction.Performer, $"You {wrenchText} the {gameObject.ExpensiveName()}");
				return;
			}
			explosiveGUI.ServerPerformInteraction(interaction);
		}
	}
}