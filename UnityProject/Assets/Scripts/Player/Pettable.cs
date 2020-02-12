using UnityEngine;

/// <summary>
/// Allows an object to be pet by a player. Shameless copy of Huggable.cs
/// </summary>
public class Pettable : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public bool WillInteract( PositionalHandApply interaction, NetworkSide side )
	{
		var targetNPCHealth = interaction.TargetObject.GetComponent<LivingHealthBehaviour>();
		var performerPlayerHealth = interaction.Performer.GetComponent<PlayerHealth>();
		var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();

		if (Validations.CanApply(interaction.Performer, interaction.TargetObject, side, true, ReachRange.Standard, interaction.TargetVector))
		{
			return true;
		}
		return false;
	}

	
	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer,
		$"You pet {interaction.TargetObject.name}.", $"{interaction.Performer.ExpensiveName()} pets {interaction.TargetObject.name}.");
	}
}
