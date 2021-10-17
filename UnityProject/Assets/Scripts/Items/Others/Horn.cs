using System.Collections;
using UnityEngine;
using HealthV2;
using Messages.Server.SoundMessages;

namespace Items
{
	/// <summary>
	/// Indicates an object that emits sound upon activation (bike horn/air horn...)
	/// </summary>
	public class Horn : MonoBehaviour, ICheckedInteractable<HandActivate>, ICheckedInteractable<PositionalHandApply>
	{
		[SerializeField]
		private float Cooldown = 0.2f;

		//todo: emit HONK particles

		/// <summary>
		/// Chance of causing minor ear injury when honking next to a living being
		/// </summary>
		[SerializeField, Range(0f, 1f)]
		private float CritChance = 0.1f;

		[SerializeField, Range(0f, 100f)]
		private float CritDamage = 5f;

		private bool allowUse = true;
		private float randomPitch => Random.Range(0.7f, 1.2f);

		private IEnumerator StartCooldown()
		{
			allowUse = false;
			yield return WaitFor.Seconds(Cooldown);
			allowUse = true;
		}

		private IEnumerator CritHonk(PositionalHandApply clickData, LivingHealthMasterBase livingHealth)
		{
			yield return WaitFor.Seconds(0.02f);
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: -1f); //This plays it backwards, is that what you wanted?
			ShakeParameters shakeParameters = new ShakeParameters(true, 20, 5);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.ClownHonk, gameObject.AssumedWorldPosServer(), audioSourceParameters, true, sourceObj: GetHonkSoundObject(), shakeParameters: shakeParameters);
			livingHealth.ApplyDamageToBodyPart(clickData.Performer, CritDamage, AttackType.Energy, DamageType.Brute, BodyPartType.Head);
		}

		/// <summary>
		///	honk on activate
		/// </summary>
		public void ServerPerformInteraction(HandActivate interaction)
		{
			ClassicHonk();
			StartCoroutine(StartCooldown());
		}

		/// <summary>
		/// honk on world click
		/// </summary>
		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			Vector3 performerWorldPos = interaction.PerformerPlayerScript.WorldPos;
			bool inCloseRange = Validations.IsReachableByPositions(performerWorldPos, performerWorldPos + (Vector3)interaction.TargetVector, true, context: interaction.TargetObject);
			var targetObject = interaction.TargetObject;
			var targetHealth = targetObject != null ? targetObject.GetComponent<LivingHealthMasterBase>() : null;
			bool isCrit = Random.Range(0f, 1f) <= CritChance;

			// honking in someone's face
			if (inCloseRange && (targetHealth != null))
			{
				interaction.Performer.GetComponent<WeaponNetworkActions>().RpcMeleeAttackLerp(interaction.TargetVector, gameObject);
				Chat.AddAttackMsgToChat(interaction.Performer, targetObject, BodyPartType.None, gameObject);

				ClassicHonk();

				if (isCrit && interaction.Intent == Intent.Harm)
				{
					StartCoroutine(CritHonk(interaction, targetHealth));
				}
			}
			else
			{
				ClassicHonk();
			}

			StartCoroutine(StartCooldown());
		}

		public void ClassicHonk()
		{
			AudioSourceParameters hornParameters = new AudioSourceParameters(pitch: randomPitch);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.ClownHonk, gameObject.AssumedWorldPosServer(), hornParameters, true, sourceObj: GetHonkSoundObject());
		}

		/// <summary>
		/// Allow honking when barely conscious
		/// </summary>
		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return Validations.CanInteract(interaction.PerformerPlayerScript, side, true)
				   && allowUse;
		}

		/// <summary>
		/// Allow honking when barely conscious and when clicking anything
		/// </summary>
		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (interaction.HandObject != gameObject) return false;
			return Validations.CanApply(interaction.PerformerPlayerScript, interaction.TargetObject, side, true, ReachRange.Unlimited, interaction.TargetVector)
					&& allowUse;
		}

		/// <summary>
		/// Is called to find the object where the honk sound is played.
		/// </summary>
		/// <returns>The GameObject where the sound for the Honk should be played.
		/// If the horn is in an inventory, the container in which it is located is returned. </returns>
		private GameObject GetHonkSoundObject()
		{
			ItemSlot itemslot = gameObject.GetComponent<Pickupable>().ItemSlot;
			return itemslot != null ? itemslot.ItemStorage.GetRootStorageOrPlayer() : gameObject;
		}
	}
}
