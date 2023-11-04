using System.Collections;
using System.Collections.Generic;
using Clothing;
using HealthV2;
using Items;
using UnityEngine;

public class CoreEnhancer : MonoBehaviour
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.SelfHeal);

	public virtual bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (Validations.HasComponent<SlimeCore>(interaction.TargetObject) == false) return false;
		return interaction.Intent == Intent.Help;
	}

	public virtual void ServerPerformInteraction(HandApply interaction)
	{
		if (CheckTarget(interaction.TargetObject))
		{
			void ProgressComplete()
			{
				ServerApplyPotion(interaction.TargetObject);
			}

			StandardProgressAction.Create(ProgressConfig, ProgressComplete)
				.ServerStartProgress(interaction.Performer.RegisterTile(), 5f,
					interaction.Performer); //TODO Think about
		}

	}


	public void ServerApplyPotion(GameObject Target)
	{
		if (CheckTarget(Target))
		{

			var SlimeCore = Target.GetComponent<SlimeCore>();
			SlimeCore.Enhanced = true;
			SlimeCore.EnhancedUsedUp = false;

			_ = Despawn.ServerSingle(this.gameObject);

		}
	}


	public bool CheckTarget(GameObject Target)
	{
		var SlimeCore = Target.GetComponent<SlimeCore>();
		if (SlimeCore == null) return false;
		if (SlimeCore.Enhanced) return false;

		return true;

	}
}
