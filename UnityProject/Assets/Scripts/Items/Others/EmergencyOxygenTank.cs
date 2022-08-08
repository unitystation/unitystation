using System;
using Objects.Atmospherics;
using UnityEngine;

namespace Items.Others
{
	public class EmergencyOxygenTank : MonoBehaviour, IPredictedInteractable<HandActivate>
	{
		private GasContainer container;
		[SerializeField] private float gasReleaseOnPush = 10f;

		private void Awake()
		{
			container = GetComponent<GasContainer>();
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if(container.FullPercentage <= 1f) return;
			Jetpack.PushPlayerInFacedDirection(interaction.PerformerPlayerScript, container, gasReleaseOnPush);
		}

		public void ClientPredictInteraction(HandActivate interaction)
		{
			if(container.FullPercentage <= 1f) return;
			Jetpack.PushPlayerInFacedDirection(interaction.PerformerPlayerScript, container, gasReleaseOnPush);
			Chat.AddExamineMsg(interaction.Performer, "You release some of the gas in this tank.");
		}

		public void ServerRollbackClient(HandActivate interaction)
		{
			interaction.PerformerPlayerScript.playerMove.ResetLocationOnClients();
		}
	}
}
