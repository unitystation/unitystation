using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class MutationInjector : MonoBehaviour , ICheckedInteractable<PositionalHandApply>
{
	public List<DNAMutationData> DNAPayload = new List<DNAMutationData>();


	public SpriteHandler SpriteHandler;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject == gameObject) return false;
		if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) == false) return false;
		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var LHB  = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		if (LHB != null)
		{
			LHB.InjectDNA(DNAPayload);
			SpriteHandler.ChangeSprite(1);
		}
	}


}
