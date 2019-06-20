using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Component which allows this object to be applied to a living thing, healing it.
/// </summary>
public class HealsTheLiving : NBHandApplyInteractable
{
	public DamageType healType;
	private bool isSelfHealing;
	private int amount = 6; //TODO: move into some stack component (metal sheets, ores, etc)

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;
		//can only be applied to LHB
		if (!Validations.HasComponent<LivingHealthBehaviour>(interaction.TargetObject)) return false;
		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
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
		amount--;
		if(amount == 0)
		{
			GetComponent<CustomNetTransform>().DisappearFromWorldServer();
		}
	}

	[Server]
	private void SelfHeal(GameObject originator, BodyPartBehaviour targetBodyPart)
	{
		if (!isSelfHealing)
		{
			var progressFinishAction = new FinishProgressAction(
				reason =>
				{
					if (reason == FinishProgressAction.FinishReason.INTERRUPTED)
					{
						isSelfHealing = false;
					}
					else if (reason == FinishProgressAction.FinishReason.COMPLETED)
					{
						ApplyHeal(targetBodyPart);
						isSelfHealing = false;
					}
				}
			);
			isSelfHealing = true;
			UIManager.ProgressBar.StartProgress(originator.transform.position.RoundToInt(), 5f, progressFinishAction, originator);
		}
	}
}
