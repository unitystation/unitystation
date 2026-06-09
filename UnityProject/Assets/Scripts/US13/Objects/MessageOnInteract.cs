using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;

namespace US13.Objects
{
	/// <summary>
	/// Component which causes the server to send an examine message to the player who clicks the object it's on.
	/// </summary>
	public class MessageOnInteract : MonoBehaviour, IInteractable<HandApply>
	{
		[SerializeField]
		[TextArea(3,5)]
		public string Message;

		public void ServerPerformInteraction(HandApply interaction)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, Message);
		}
	}
}
