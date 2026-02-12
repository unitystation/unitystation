using UnityEngine;
using US13.Core.Lifecycle;
using US13.Objects.Gateway;

namespace US13.Systems.MaintRooms
{
	public class MaintRoomsMarker : MonoBehaviour, IServerSpawn
	{
		public void OnSpawnServer(SpawnInfo spawnInfo)
		{
			TransportUtility.MaintRoomLocations.Add(this.gameObject);
		}
	}
}
