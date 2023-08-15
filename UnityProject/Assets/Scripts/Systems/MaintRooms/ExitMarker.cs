using UnityEngine;

namespace Systems.Scenes
{
	public class ExitMarker : MonoBehaviour, IServerSpawn
	{
		public void OnSpawnServer(SpawnInfo spawnInfo)
		{
			MaintGeneratorManager.possibleExits.Add(gameObject);
		}
	}
}
