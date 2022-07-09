using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using Health.Sickness;
using HealthV2;
using Mirror;
using UnityEngine;

public class Syringe : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public ReagentContainer LocalContainer;

	public List<SicknessAffliction> SicknessesInSyringe = new List<SicknessAffliction>();

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) == false) return false;

		return true;
	}

	/// <summary>
	/// Server handles hand interaction with tray
	/// </summary>
	public void ServerPerformInteraction(HandApply interaction)
	{
		var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		if (LHB != null)
		{
			if (LocalContainer.ReagentMixTotal > 0)
			{
				LHB.CirculatorySystem.BloodPool.Add(LocalContainer.TakeReagents(15f));
				Chat.AddActionMsgToChat(interaction.Performer, $"You Inject The syringe into {LHB.gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} injects a syringe into {LHB.gameObject.ExpensiveName()}");
				if(SicknessesInSyringe.Count > 0) LHB.AddSickness(SicknessesInSyringe.PickRandom().Sickness);
			}
			else
			{
				LocalContainer.Add(LHB.CirculatorySystem.BloodPool.Take(15f));
				Chat.AddActionMsgToChat(interaction.Performer, $"You pull the blood from {LHB.gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} pulls the blood from {LHB.gameObject.ExpensiveName()}");
				if(LHB.mobSickness.sicknessAfflictions.Count > 0) SicknessesInSyringe.AddRange(LHB.mobSickness.sicknessAfflictions);
			}
		}
	}
}