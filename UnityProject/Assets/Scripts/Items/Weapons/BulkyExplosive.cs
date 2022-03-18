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
		[SerializeField] private AddressableAudioSource beepSound;

		private void OnDisable()
		{
			Emitter = null;
		}

		[Server]
		public override IEnumerator Countdown()
		{
			countDownActive = true;
			spriteHandler.SetSpriteSO(activeSpriteSO);
			if (GUI != null) GUI.StartCoroutine(GUI.UpdateTimer());
			StartCoroutine(BeepBeep());
			yield return WaitFor.Seconds(timeToDetonate); //Delay is in milliseconds
			countDownActive = false;
			Detonate();
		}

		private IEnumerator BeepBeep()
		{
			while (countDownActive && gameObject != null)
			{
				SoundManager.PlayNetworkedAtPos(beepSound, gameObject.AssumedWorldPosServer());
				yield return WaitFor.Seconds(2f);
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
			if (interaction.HandObject != null && interaction.HandObject.Item().HasTrait(wrenchTrait))
			{
				objectBehaviour.ServerSetPushable(!objectBehaviour.IsPushable);
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Wrench, gameObject.AssumedWorldPosServer());
				var wrenchText = objectBehaviour.IsPushable ? "wrench down" : "unwrench";
				Chat.AddExamineMsg(interaction.Performer, $"You {wrenchText} the {gameObject.ExpensiveName()}");
				return;
			}
			if (interaction.HandObject != null && interaction.HandObject.TryGetComponent<SignalEmitter>(out var emitter))
			{
				PairEmitter(emitter, interaction.Performer);
				return;
			}
			explosiveGUI.ServerPerformInteraction(interaction);
		}
	}
}