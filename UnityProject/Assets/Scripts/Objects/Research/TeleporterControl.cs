using Items;
using UnityEngine;

namespace Objects.Research
{
	/// <summary>
	/// Teleporter console
	/// </summary>
	public class TeleporterControl : TeleporterBase, IServerSpawn, ICheckedInteractable<HandApply>
	{
		public void OnSpawnServer(SpawnInfo info)
		{
			SetControl(this);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.HandObject != null) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(connectedStation == null) return;

			connectedStation.SetBeacon(TrackingBeacon.GetAllBeaconOfType(TrackingBeacon.TrackingBeaconTypes.All).PickRandom());
		}
	}
}