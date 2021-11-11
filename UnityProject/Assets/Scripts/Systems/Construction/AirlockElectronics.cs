using UnityEngine;
using Messages.Server;

namespace Items.Construction
{
	public class AirlockElectronics : MonoBehaviour, IInteractable<HandActivate>
	{
		[SerializeField]
		[Tooltip("Current airlock access.")]
		private Access currentAccess;

		public Access CurrentAccess
		{
			get => currentAccess;
			set => currentAccess = value;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			//show the UI to the client
			TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.AirlockElectronics, TabAction.Open);
		}
	}

}
