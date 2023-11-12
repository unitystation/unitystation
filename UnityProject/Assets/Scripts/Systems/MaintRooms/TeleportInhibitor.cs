using System.Collections.Generic;
using UnityEngine;

namespace Systems.Scenes
{
	public class TeleportInhibitor : MonoBehaviour, IServerSpawn //Used to restrict teleports from handteles within a specified range. Could be expanded in future to cover other methods of teleportation.
	{
		[field:SerializeField] public int Range { get; private set; } = 10; 
		internal readonly static List<TeleportInhibitor> Inhibitors = new List<TeleportInhibitor>();

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
