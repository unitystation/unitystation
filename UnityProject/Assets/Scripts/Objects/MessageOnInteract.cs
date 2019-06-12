using UnityEngine;

/// <summary>
/// Component which causes the server to send an examine message to the player who clicks the object it's on.
/// </summary>
public class MessageOnInteract : NBHandApplyInteractable
{
	public string Message;

	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return CommonValidationChains.CAN_APPLY_HAND_SOFT_CRIT;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		UpdateChatMessage.Send(interaction.Performer, ChatChannel.Examine, Message);
	}
}
