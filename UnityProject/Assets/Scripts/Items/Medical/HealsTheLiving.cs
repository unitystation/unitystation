﻿using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using Items;
 using Messages.Server.HealthMessages;

 /// <summary>
/// Component which allows this object to be applied to a living thing, healing it.
/// </summary>
[RequireComponent(typeof(Stackable))]
public class HealsTheLiving : MonoBehaviour, ICheckedInteractable<HandApply>
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.SelfHeal);

	public DamageType healType;

	public bool StopsExternalBleeding = false;
	public bool HealsSlashDamage = false;

	[Range(0,100)] public float SlashDamageToHeal = 20;

	protected Stackable stackable;

	private void Awake()
	{
		stackable = GetComponent<Stackable>();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only be applied to LHB
		if (!Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject)) return false;

		if(interaction.Intent != Intent.Help) return false;
		return true;
	}

	public virtual void ServerPerformInteraction(HandApply interaction)
	{
		var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		if (LHB.ZoneHasDamageOf(interaction.TargetBodyPart,healType))
		{
			if (interaction.TargetObject != interaction.Performer)
			{
				ServerApplyHeal(LHB,interaction);
			}
			else
			{
				ServerSelfHeal(interaction.Performer,LHB, interaction);
			}
		}
		else
		{
			//If there is no limb in this Zone, check if it's bleeding from limb loss.
			if(CheckForBleedingBodyContainers(LHB, interaction) && StopsExternalBleeding)
			{
				RemoveLimbLossBleed(LHB, interaction);
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"The {interaction.TargetBodyPart} does not need to be healed.");
			}
		}
	}

	private void ServerApplyHeal(LivingHealthMasterBase targetBodyPart, HandApply interaction)
	{
		targetBodyPart.HealDamage(null, 40, healType, interaction.TargetBodyPart);
		RemoveLimbLossBleed(targetBodyPart, interaction);
		stackable.ServerConsume(1);
	}

	private void ServerSelfHeal(GameObject originator, LivingHealthMasterBase livingHealthMasterBase, HandApply interaction)
	{
		void ProgressComplete()
		{
			ServerApplyHeal(livingHealthMasterBase, interaction);
		}

		StandardProgressAction.Create(ProgressConfig, ProgressComplete)
			.ServerStartProgress(originator.RegisterTile(), 5f, originator);
	}

	protected bool CheckForBleedingBodyContainers(LivingHealthMasterBase targetBodyPart, HandApply interaction)
	{
		foreach(var container in targetBodyPart.RootBodyPartContainers)
		{
			if(container.BodyPartType == interaction.TargetBodyPart && container.IsBleeding == true)
			{
				return true;
			}
		}
		return false;
	}

	protected void RemoveLimbLossBleed(LivingHealthMasterBase targetBodyPart, HandApply interaction)
	{
		foreach(var container in targetBodyPart.RootBodyPartContainers)
		{
			if(container.BodyPartType == interaction.TargetBodyPart && container.IsBleeding == true)
			{
				container.IsBleeding = false;
				Chat.AddActionMsgToChat(interaction.Performer.gameObject, 
				$"You stopped {interaction.TargetObject.ExpensiveName()}'s bleeding.",
				$"{interaction.PerformerPlayerScript.visibleName} stopped {interaction.TargetObject.ExpensiveName()}'s bleeding.");
			}
		}
	}
}