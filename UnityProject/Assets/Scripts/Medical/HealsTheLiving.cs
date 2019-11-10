using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Component which allows this object to be applied to a living thing, healing it.
/// </summary>
public class HealsTheLiving : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
{
	public DamageType healType;
	//total number of times this can be used
	public int uses = 6;  //TODO: move into some stack component (metal sheets, ores, etc)
	private int timesUsed;

	public void OnSpawnServer(SpawnInfo info)
	{
		timesUsed = 0;
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only be applied to LHB
		if (!Validations.HasComponent<LivingHealthBehaviour>(interaction.TargetObject)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var LHB = interaction.TargetObject.GetComponent<LivingHealthBehaviour>();
		if (LHB.IsDead)
		{
			return;
		}
		var targetBodyPart = LHB.FindBodyPart(interaction.TargetBodyPart);
		if (targetBodyPart.GetDamageValue(healType) > 0)
		{
			if (interaction.TargetObject != interaction.Performer)
			{
				ApplyHeal(targetBodyPart);
			}
			else
			{
				SelfHeal(interaction.Performer, targetBodyPart);
			}
		}
	}

	[Server]
	private void ApplyHeal(BodyPartBehaviour targetBodyPart)
	{
		targetBodyPart.HealDamage(40, healType);
		timesUsed++;
		Logger.LogTraceFormat("{0} uses left.", Category.Health, uses - timesUsed);
		if(uses == timesUsed)
		{
			Despawn.ServerSingle(gameObject);
		}
	}

	[Server]
	private void SelfHeal(GameObject originator, BodyPartBehaviour targetBodyPart)
	{
		var progressFinishAction = new ProgressCompleteAction(() => ApplyHeal(targetBodyPart));
		UIManager.ServerStartProgress(ProgressAction.SelfHeal, originator.transform.position.RoundToInt(), 5f, progressFinishAction, originator);
	}
}
