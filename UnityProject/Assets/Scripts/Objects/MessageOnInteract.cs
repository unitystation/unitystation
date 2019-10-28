using UnityEngine;

/// <summary>
/// Component which causes the server to send an examine message to the player who clicks the object it's on.
/// </summary>
public class MessageOnInteract : MonoBehaviour, IInteractable<HandApply>
{
	public string Message;

	public void ServerPerformInteraction(HandApply interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer, Message);
	}
}
