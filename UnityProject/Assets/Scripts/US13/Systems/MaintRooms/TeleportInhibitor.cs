using System.Collections.Generic;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers.NetworkManagement;

namespace US13.Systems.MaintRooms
{
	public class TeleportInhibitor : MonoBehaviour, IServerSpawn //Used to restrict teleports from handteles within a specified range. Could be expanded in future to cover other methods of teleportation.
	{
		[field:SerializeField] public int Range { get; private set; } = 10;
		public readonly static List<TeleportInhibitor> Inhibitors = new List<TeleportInhibitor>();

		private void OnDestroy()
		{
			if(CustomNetworkManager.IsServer) Inhibitors.Remove(this);
		}

		public void OnSpawnServer(SpawnInfo spawnInfo)
		{
			Inhibitors.Add(this);
		}
	}
}
