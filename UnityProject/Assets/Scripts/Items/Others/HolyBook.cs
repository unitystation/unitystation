﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = System.Random;
/// <summary>
/// Component which allows this object to heal or cause brain damage if used by the Chaplain.
/// </summary>
public class HolyBook: MonoBehaviour, IPredictedCheckedInteractable<PositionalHandApply>
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.SelfHeal);

	private static Random rnd = new Random();

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
		//can only be applied to LHB
		if (!Validations.HasComponent<LivingHealthBehaviour>(interaction.TargetObject)) return;

		//The book can't save people who are dead.
		var LHB = interaction.TargetObject.GetComponent<LivingHealthBehaviour>();

		if (LHB.IsDead)
		{
			return;
		}

		//Occurs only if applied on the head.
		if (interaction.TargetBodyPart != BodyPartType.Head) return;

		//Only the Chaplain can use the holy book.
		if (PlayerList.Instance.Get(interaction.Performer).Job != JobType.CHAPLAIN)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "A force restrains you. Non-Clergymen can't use this!");
			return;
		}

		//If using harm intent, book has 60% chance of causing brain trauma, and 40% of healing a bodypart.
		//If using any other intent, book has 60% chance of healing a bodypart, and 40% of causing brain trauma.

		int rand = rnd.Next(0, 10);
		bool willHarm = false;
		switch (interaction.Intent)
		{
		default:
		case Intent.Help:
			if (rand <= 5)
			{
				willHarm = true;
			}
			else
			{
				willHarm = false;
			}
			break;
		case Intent.Harm:

			if (rand <= 5)
			{
				willHarm = false;
			}
			else
			{
				willHarm = true;
			}
			break;
		}


		string performerName = interaction.Performer.ExpensiveName();
		string victimName = interaction.TargetObject.ExpensiveName();
		//Deal 10 brain trauma. [DUE TO NOT HAVING BRAIN DAMAGE YET, SUFFOCATION IS USED TO PREVENT SPAMMING.]
		//TODO: Rewrite this to deal 10 brain damage when organ damage is implemented.
		if (willHarm)
		{
			LHB.bloodSystem.OxygenDamage += healthModifier;

			Chat.AddActionMsgToChat(interaction.Performer, $"Your book slams into {victimName}'s head, and not much else.",
			$"{performerName}'s book slams into {victimName}'s head, and not much else.");

			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.GenericHit, interaction.WorldPositionTarget, sourceObj: interaction.Performer);
		}
		else  //Heal a bodypart if possible.
		{
			//If there is no damage, do nothing.
			if (!(LHB.OverallHealth >= LHB.maxHealth))
			{
				//Break foreach loop once a single heal is applied.
				foreach (BodyPartBehaviour bodyPart in LHB.BodyParts)
				{
					//Heal brute first, then burns.
					if (bodyPart.BruteDamage != 0)
					{
						bodyPart.HealDamage(healthModifier, DamageType.Brute);
						break;
					}

					if (bodyPart.BurnDamage != 0)
					{
						bodyPart.HealDamage(healthModifier, DamageType.Burn);
						break;
					}
				}

				Chat.AddActionMsgToChat(interaction.Performer,
				$"A flash of light from your book thwacking {victimName} heals some of {victimName}'s  wounds.",
				$"A flash of light from {performerName}'s book thwacking {victimName} heals some of {victimName}'s wounds.");

				SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.PunchMiss, interaction.WorldPositionTarget, sourceObj: interaction.Performer);
			}


		}

		//Play melee animation.
		interaction.Performer.GetComponent<WeaponNetworkActions>().RpcMeleeAttackLerp(interaction.TargetVector, gameObject);

		//Start server cooldown.
		Cooldowns.TryStartServer(interaction.Performer.GetComponent<PlayerScript>(), CommonCooldowns.Instance.Melee);

	}





}