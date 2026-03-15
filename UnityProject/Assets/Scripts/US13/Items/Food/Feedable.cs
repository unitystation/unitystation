using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.HealthV2;
using Util;

namespace US13.Items.Food
{
	/// <summary>
	/// This makes it possible to always eat something that is edible but might have other interactions implementors that consume
	/// the eating interaction. Players have to aim to the mouth for this to work.
	/// </summary>
	public class Feedable : MonoBehaviour, IFirstInteractable<HandApply>
	{
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.TargetBodyPart != BodyPartType.Mouth) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject == null) return false;
			if (!interaction.HandObject.TryGetComponentCustom<Edible>(out _)) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var edible = interaction.HandObject.GetComponentCustom<Edible>();
			edible.TryConsume(interaction.Performer, interaction.TargetObject);
		}
	}
}