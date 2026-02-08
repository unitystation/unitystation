using System.Text;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using Util;

namespace US13.Systems.Research.LaserPuzzle
{
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
}
