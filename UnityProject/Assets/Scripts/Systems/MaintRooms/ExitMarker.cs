using MaintRooms;
using UnityEngine;

namespace Systems.Scenes
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
