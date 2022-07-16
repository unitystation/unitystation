using System.Linq;
using Systems.Electricity;
using Systems.ObjectConnection;
using Gateway;
using Items;
using UnityEngine;

namespace Objects.Research
{
	/// <summary>
	/// One in the middle, looks like a server rack
	/// </summary>
	public class TeleporterStation : TeleporterBase, IServerSpawn, ICheckedInteractable<HandApply>
	{
		private static readonly Vector3Int[] CardinalDirections = new[]
		{
			//Up
			new Vector3Int(0, 1, 0),
			//Right
			new Vector3Int(1, 0, 0),
			//Down
			new Vector3Int(0, -1, 0),
			//Left
			new Vector3Int(-1, 0, 0)
		};

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.HandObject != null) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			Reconnect();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			Reconnect();
		}

		private void Reconnect()
		{
			SetStation(this);

			TeleporterHub hub = null;

			for (int i = 0; i < CardinalDirections.Length; i++)
			{
				var stuff = MatrixManager.GetAt<TeleporterHub>
					((registerTile.WorldPositionServer + CardinalDirections[i]), true);

				var teleporterHubs = stuff as TeleporterHub[] ?? stuff.ToArray();
				if (teleporterHubs.Any())
				{
					hub = teleporterHubs[0];
					break;
				}
			}

			TeleporterControl control = null;

			for (int i = 0; i < CardinalDirections.Length; i++)
			{
				var stuff = MatrixManager.GetAt<TeleporterControl>
					((registerTile.WorldPositionServer + CardinalDirections[i]), true);

				var teleporterControls = stuff as TeleporterControl[] ?? stuff.ToArray();
				if (teleporterControls.Any())
				{
					control = teleporterControls[0];
					break;
				}
			}

			var test = false;

			if (hub != null)
			{
				hub.SetStation(this);

				SetHub(hub);

				if (control != null)
				{
					hub.SetControl(control);
				}

				hub.SetActive(true);

				test = true;
			}

			if (control != null)
			{
				control.SetStation(this);

				SetControl(control);

				if (hub != null)
				{
					control.SetHub(hub);
				}

				control.SetActive(true);
			}
			else
			{
				test = false;
			}

			SetActive(test);
		}
	}
}
