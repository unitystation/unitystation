using UnityEngine;
using US13.Managers.NetworkManagement;

namespace US13.Systems.MaintRooms
{
	public class ExitMarker : MonoBehaviour
	{
		public void OnEnable()
		{
			RandomExitPosition.ExitMarkers.Add(gameObject);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer) RandomExitPosition.ExitMarkers.Remove(gameObject);
		}
	}
}
