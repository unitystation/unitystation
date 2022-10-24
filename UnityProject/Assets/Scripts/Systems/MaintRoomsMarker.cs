using Gateway;
using UnityEngine;

public class MaintRoomsMarker : MonoBehaviour, IServerSpawn
{
	public void OnSpawnServer(SpawnInfo spawnInfo)
	{
		TransportUtility.MaintRoomLocations.Add(this.gameObject);
	}
}
