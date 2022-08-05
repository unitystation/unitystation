using System;
using Objects.Atmospherics;
using UnityEngine;

namespace Items.Others
{
	public class EmergencyOxygenTank : MonoBehaviour, IPredictedInteractable<HandActivate>
	{
		private GasContainer container;
		[SerializeField] private float gasReleaseOnPush = 10f;

		private Vector3 rollbackMemory = Vector3.zero;

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
			rollbackMemory = interaction.Performer.RegisterTile().LocalPosition;
		}

		public void ServerRollbackClient(HandActivate interaction)
		{
			interaction.Performer.RegisterTile().ServerSetLocalPosition(rollbackMemory.CutToInt());
		}
	}
}
