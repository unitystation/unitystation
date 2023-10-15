using System.Linq;
using HealthV2;
using UnityEngine;
using Items;

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

	[SerializeField]
	protected TraumaticDamageTypes TraumaTypeToHeal;

	protected Stackable stackable;

	private void Awake()
	{
		stackable = GetComponent<Stackable>();
	}

	public virtual bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (!Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject)) return false;
		if (interaction.TargetObject.GetComponent<Dissectible>().GetBodyPartIsopen && interaction.IsAltClick == false) return false;

		return interaction.Intent == Intent.Help;
	}

	public virtual void ServerPerformInteraction(HandApply interaction)
	{
		if (TryGetComponent<HandPreparable>(out var preparable))
		{
			if (preparable.IsPrepared == false)
			{
				Chat.AddExamineMsg(interaction.Performer, preparable.openingRequirementText);
				return;
			}
		}
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
			if (CheckForBleedingBodyContainers(LHB, interaction) && StopsExternalBleeding)
			{
				RemoveLimbLossBleed(LHB, interaction);
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"The {interaction.TargetBodyPart} does not need to be healed.");
			}
		}

		//(MAX): TEMPORARY.
		//TODO: Add proper trauma healing.
		if (HasTrauma(LHB)) HealTrauma(LHB, interaction);
	}

	private void ServerApplyHeal(LivingHealthMasterBase livingHealth, HandApply interaction)
	{
		livingHealth.HealDamage(null, 40, healType, interaction.TargetBodyPart, true);
		if (StopsExternalBleeding)
		{
			RemoveLimbLossBleed(livingHealth, interaction);
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

	protected bool HasTrauma(LivingHealthMasterBase health)
	{
		if (health.gameObject.TryGetComponent<CreatureTraumaManager>(out var traumaManager) == false) return false;
		return traumaManager.HasAnyTraumaOfType(TraumaTypeToHeal) == false;
	}

	protected virtual void HealTrauma(LivingHealthMasterBase health, HandApply interaction)
	{
		if (health.gameObject.TryGetComponent<CreatureTraumaManager>(out var traumaManager) == false) return;
		var healedTrauma = false;
		foreach (var bodyPart in health.BodyPartList)
		{
			if (traumaManager.HealBodyPartTrauma(bodyPart, TraumaTypeToHeal)) healedTrauma = true;
		}
		if(healedTrauma) stackable.ServerConsume(1);
	}

	protected void RemoveLimbLossBleed(LivingHealthMasterBase livingHealth, HandApply interaction)
	{
		foreach(var bodyPart in livingHealth.BodyPartList)
		{
			if(bodyPart.BodyPartType == interaction.TargetBodyPart && bodyPart.IsBleeding == true)
			{
				bodyPart.IsBleeding = false;
				livingHealth.SetBleedStacks(0f);
				Chat.AddActionMsgToChat(interaction.Performer.gameObject,
				$"You stopped {interaction.TargetObject.ExpensiveName()}'s bleeding.",
				$"{interaction.PerformerPlayerScript.visibleName} stopped {interaction.TargetObject.ExpensiveName()}'s bleeding.");
			}
		}
	}
}

/// NOTE FROM MAX ///
/// this script really needs to be re-thought out ///
/// away from the fact it looks ugly, it seems to be poorly designed or tries to do multiple things at once ///
/// I would fix it right now, but I've spent so much time cleaning other stuff; I'd be here all month just cleaning old bad code ///