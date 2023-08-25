using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class MutationInjector : Syringe
{
	public List<DNAMutationData> DNAPayload = new List<DNAMutationData>();
	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		if (LHB != null)
		{
			LHB.InjectDna(DNAPayload);
			SpriteHandler.ChangeSprite(1);
		}
	}

	public override void InjectBehavior(LivingHealthMasterBase LHB, RegisterPlayer performer)
	{
		Chat.AddCombatMsgToChat(performer.gameObject,
			$"You Inject The {this.name} into {LHB.gameObject.ExpensiveName()}",
			$"{performer.PlayerScript.visibleName} injects a {this.name} into {LHB.gameObject.ExpensiveName()}");
		if (SicknessesInSyringe.Count > 0) LHB.AddSickness(SicknessesInSyringe.PickRandom().Sickness);
		LHB.InjectDna(DNAPayload);

		SpriteHandler.ChangeSprite(1);
	}
}