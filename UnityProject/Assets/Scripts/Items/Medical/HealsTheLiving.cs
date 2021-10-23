using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using NaughtyAttributes;
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
	public bool HealsTraumaDamage = false;

	[Range(0,100), EnableIf("HealsTraumaDamage")]
	public float TraumaDamageToHeal = 20;

	[SerializeField, EnableIf("HealsTraumaDamage")]
	protected TraumaticDamageTypes TraumaTypeToHeal;

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

	private void ServerApplyHeal(LivingHealthMasterBase livingHealth, HandApply interaction)
	{
		livingHealth.HealDamage(null, 40, healType, interaction.TargetBodyPart);
		if (StopsExternalBleeding)
		{
			RemoveLimbLossBleed(livingHealth, interaction);
		}
		if (HealsTraumaDamage)
		{
			HealTraumaDamage(livingHealth, interaction);
		}
		stackable.ServerConsume(1);
	}

	private void ServerSelfHeal(GameObject originator, LivingHealthMasterBase livingHealth, HandApply interaction)
	{
		void ProgressComplete()
		{
			ServerApplyHeal(livingHealth, interaction);
		}

		StandardProgressAction.Create(ProgressConfig, ProgressComplete)
			.ServerStartProgress(originator.RegisterTile(), 5f, originator);
	}

	protected void HealTraumaDamage(LivingHealthMasterBase livingHealth, HandApply interaction)
	{
		if (livingHealth.HasTraumaDamage(interaction.TargetBodyPart))
		{
			livingHealth.HealTraumaDamage(interaction.TargetBodyPart, TraumaTypeToHeal);
			Chat.AddActionMsgToChat(interaction,
			$"You apply the {gameObject.ExpensiveName()} to {livingHealth.playerScript.visibleName}",
			$"{interaction.Performer.ExpensiveName()} applies {name} to {livingHealth.playerScript.visibleName}.");
		}
	}

	protected bool CheckForBleedingBodyContainers(LivingHealthMasterBase livingHealth, HandApply interaction)
	{
		foreach(var bodyPart in livingHealth.BodyPartList)
		{
			if(bodyPart.BodyPartType == interaction.TargetBodyPart && bodyPart.IsBleeding == true)
			{
				return true;
			}
		}
		return false;
	}

	protected void RemoveLimbLossBleed(LivingHealthMasterBase livingHealth, HandApply interaction)
	{
		foreach(var bodyPart in livingHealth.BodyPartList)
		{
			if(bodyPart.BodyPartType == interaction.TargetBodyPart && bodyPart.IsBleeding == true)
			{
				bodyPart.IsBleeding = false;
				Chat.AddActionMsgToChat(interaction.Performer.gameObject,
				$"You stopped {interaction.TargetObject.ExpensiveName()}'s bleeding.",
				$"{interaction.PerformerPlayerScript.visibleName} stopped {interaction.TargetObject.ExpensiveName()}'s bleeding.");
			}
		}
	}
}