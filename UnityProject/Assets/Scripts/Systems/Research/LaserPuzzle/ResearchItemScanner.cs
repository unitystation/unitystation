using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ResearchItemScanner : MonoBehaviour,	ICheckedInteractable<PositionalHandApply>
{
	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		if (interaction.TargetObject == gameObject) return false;

		if (Validations.HasComponent<ItemResearchPotential>(interaction.TargetObject) == false) return false;

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		StringBuilder Stringy = new StringBuilder();
		var Research = interaction.TargetObject.GetComponent<ItemResearchPotential>();

		var Purity = Research.CurrentPurity;


		Stringy.AppendLine($" Item {interaction.TargetObject.gameObject.ExpensiveName()} has a purity of {Purity}");
		Stringy.AppendLine($" Also contains { Research.TechWebDesigns.Count} as potential technologies");

		if (Research.IsTooPure)
		{
			Stringy.AppendLine($" this is the purest sample we've seen yet. and potentially the most unstable. ");
		}


		Chat.AddExamineMsgFromServer(interaction.Performer, Stringy.ToString());
	}
}
