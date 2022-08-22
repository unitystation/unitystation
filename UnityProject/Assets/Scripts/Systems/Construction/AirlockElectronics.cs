using UnityEngine;
using Messages.Server;
using Systems.Clearance;

namespace Items.Construction
{
	public class AirlockElectronics : MonoBehaviour, IInteractable<HandActivate>
	{
		[SerializeField]
		[Tooltip("Current airlock access.")]
		private Clearance currentClearance = Clearance.MaintTunnels;

		public Clearance CurrentClearance
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
