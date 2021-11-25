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
	public class TeleporterStation : TeleporterBase, IServerSpawn
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

		public void OnSpawnServer(SpawnInfo info)
		{
			TeleporterHub hub = null;

			for (int i = 0; i < CardinalDirections.Length; i++)
			{
				var stuff = MatrixManager.GetAt<TeleporterHub>
					((registerTile.WorldPositionServer + CardinalDirections[i]), true);

				if (stuff.Count > 0)
				{
					hub = stuff[0];
					break;
				}
			}

			TeleporterControl control = null;

			for (int i = 0; i < CardinalDirections.Length; i++)
			{
				var stuff = MatrixManager.GetAt<TeleporterControl>
					((registerTile.WorldPositionServer + CardinalDirections[i]), true);

				if (stuff.Count > 0)
				{
					control = stuff[0];
					break;
				}
			}

			if (hub != null)
			{
				hub.connectedStation = this;

				connectedHub = hub;

				if (control != null)
				{
					hub.connectedControl = control;
				}
			}

			if (control != null)
			{
				control.connectedStation = this;

				connectedControl = control;

				if (hub != null)
				{
					control.connectedHub = hub;
				}
			}
		}
	}
}
