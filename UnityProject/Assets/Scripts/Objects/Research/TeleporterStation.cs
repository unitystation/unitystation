using System.Linq;
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
			Chat.AddLocalMsgToChat("Connecting to local devices...", gameObject);

			SetStation(this);

			TeleporterHub hub = null;
			var foundHub = false;

			for (int i = 0; i < CardinalDirections.Length; i++)
			{
				var stuff = MatrixManager.GetAt<TeleporterHub>
					((registerTile.WorldPositionServer + CardinalDirections[i]), true);

				var teleporterHubs = stuff as TeleporterHub[] ?? stuff.ToArray();
				if (teleporterHubs.Any())
				{
					hub = teleporterHubs[0];
					foundHub = true;
					break;
				}
			}

			TeleporterControl control = null;
			var foundControl = false;

			for (int i = 0; i < CardinalDirections.Length; i++)
			{
				var stuff = MatrixManager.GetAt<TeleporterControl>
					((registerTile.WorldPositionServer + CardinalDirections[i]), true);

				var teleporterControls = stuff as TeleporterControl[] ?? stuff.ToArray();
				if (teleporterControls.Any())
				{
					control = teleporterControls[0];
					foundControl = true;
					break;
				}
			}

			var test = false;

			if (foundHub)
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

			if (foundControl)
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

			Chat.AddActionMsgToChat(gameObject, foundHub ? "Teleporter Hub found and connected..." : "Teleporter Hub not found!");

			Chat.AddActionMsgToChat(gameObject, foundControl ? "Teleporter Console found and connected..." : "Teleporter Console not found!");

			Chat.AddActionMsgToChat(gameObject, test ? "All devices found!" : "Not all devices found!");

			SetActive(test);
		}
	}
}
