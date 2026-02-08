using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Messages.Server;
using US13.UI.Core.Net;

namespace US13.Systems.Construction
{
	public class AirlockElectronics : MonoBehaviour, IInteractable<HandActivate>
	{
		[SerializeField]
		[Tooltip("Current airlock access.")]
		private Clearance.Clearance currentClearance = Clearance.Clearance.MaintTunnels;

		public Clearance.Clearance CurrentClearance
		{
			get => currentClearance;
			set => currentClearance = value;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			//show the UI to the client
			TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.AirlockElectronics, TabAction.Open);
		}
	}

}
