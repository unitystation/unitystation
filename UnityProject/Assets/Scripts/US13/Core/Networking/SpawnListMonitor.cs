using NaughtyAttributes;
using UnityEngine;
using US13.Managers.NetworkManagement;

namespace US13.Core.Networking
{
	public class SpawnListMonitor : MonoBehaviour
	{
		[SerializeField] private CustomNetworkManager networkManager = null;

		[Button("Manually fill spawnable prefab list")]
		//usually dynamically filled on build
		public bool GenerateSpawnList()
		{
			networkManager.SetSpawnableList();
			return true;
		}
	}
}
