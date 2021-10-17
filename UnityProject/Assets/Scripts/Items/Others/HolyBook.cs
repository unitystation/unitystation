using System;
using System.Collections.Generic;
using UnityEngine;
using HealthV2;

namespace Items
{
	/// <summary>
	/// Component which allows this object to heal or cause brain damage if used by the Chaplain.
	/// </summary>
	public class HolyBook : MonoBehaviour, IPredictedCheckedInteractable<PositionalHandApply>
	{
		//The amount a single thwack heals or damages.
		public int healthModifier = 10;

		//Using the holy book is considered a melee attack.
		public void ClientPredictInteraction(PositionalHandApply interaction)
		{
			//start clientside melee cooldown so we don't try to spam melee
			//requests to server
			Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Melee);
		}

		//no rollback logic
		public void ServerRollbackClient(PositionalHandApply interaction) { }
		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			//Is melee on cooldown?
			if (Cooldowns.IsOn(interaction, CooldownID.Asset(CommonCooldowns.Instance.Melee, side))) return false;

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			// Can only be applied to LHB
			if (interaction.TargetObject.TryGetComponent<LivingHealthMasterBase>(out var lhb) == false) return;

			//The book can't save people who are dead.
			if (lhb.IsDead) return;

			// Occurs only if applied on the head.
			if (interaction.TargetBodyPart != BodyPartType.Head) return;

			// Only the Chaplain can use the holy book.
			if (interaction.PerformerPlayerScript.mind.occupation.JobType != JobType.CHAPLAIN)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "A force restrains you. Non-Clergymen can't use this!");
				return;
			}

			var willHarm = interaction.Intent switch
			{
				Intent.Harm => DMMath.Prob(60),
				_ => DMMath.Prob(40),
			};

			string performerName = interaction.Performer.ExpensiveName();
			string victimName = interaction.TargetObject.ExpensiveName();
			//Deal 10 brain trauma. [DUE TO NOT HAVING BRAIN DAMAGE YET, SUFFOCATION IS USED TO PREVENT SPAMMING.]
			//TODO: Rewrite this to deal 10 brain damage when organ damage is implemented.
			if (willHarm)
			{
				if (lhb.brain != null)
				{
					lhb.brain.RelatedPart.TakeDamage(this.gameObject, 10, AttackType.Magic, DamageType.Brute);
					Chat.AddActionMsgToChat(interaction.Performer, $"Your book slams into {victimName}'s head, and not much else.",
						$"{performerName}'s book slams into {victimName}'s head, and not much else.");

					SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.GenericHit, interaction.WorldPositionTarget, sourceObj: interaction.Performer);
				}
			}
			else  //Heal a bodypart if possible.
			{
				//If there is no damage, do nothing.
				if (lhb.OverallHealth < lhb.MaxHealth)
				{
					//Break foreach loop once a single heal is applied.
					foreach (BodyPart bodyPart in lhb.BodyPartList)
					{
						if (bodyPart.DamageContributesToOverallHealth == false) continue;
						//Heal brute first, then burns.
						if (bodyPart.Brute != 0)
						{
							bodyPart.HealDamage(this.gameObject, healthModifier, DamageType.Brute);
							break;
						}

						if (bodyPart.Burn != 0)
						{
							bodyPart.HealDamage(this.gameObject, healthModifier, DamageType.Burn);
							break;
						}
					}

					Chat.AddActionMsgToChat(interaction.Performer,
					$"A flash of light from your book thwacking {victimName} heals some of {victimName}'s  wounds.",
					$"A flash of light from {performerName}'s book thwacking {victimName} heals some of {victimName}'s wounds.");

					SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.PunchMiss, interaction.WorldPositionTarget, sourceObj: interaction.Performer);
				}
			}

			//Play melee animation.
			interaction.Performer.GetComponent<WeaponNetworkActions>().RpcMeleeAttackLerp(interaction.TargetVector, gameObject);

			//Start server cooldown.
			Cooldowns.TryStartServer(interaction.Performer.GetComponent<PlayerScript>(), CommonCooldowns.Instance.Melee);
		}
	}
}
