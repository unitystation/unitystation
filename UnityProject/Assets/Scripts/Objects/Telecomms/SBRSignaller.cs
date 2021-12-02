using Communications;
using Items.Devices;
using Managers;
using UnityEngine;

namespace Objects.Telecomms
{
	public class SBRSignaller : SignalReceiver, ICheckedInteractable<HandApply>
	{
		[SerializeField] private StationBouncedRadio radio;
		public override void ReceiveSignal(SignalStrength strength, ISignalMessage message = null)
		{
			radio.BroadcastToNearbyTiles = !radio.BroadcastToNearbyTiles;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject == null) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject.TryGetComponent<RemoteSignaller>(out var device))
			{
				Emitter = device;
				Chat.AddExamineMsg(interaction.Performer, $"You pair the {Emitter.gameObject.ExpensiveName()} to the {radio.gameObject.ExpensiveName()}");
			}
		}
	}
}

