using System.Collections.Generic;
using UnityEngine;

namespace US13.Systems.MaintRooms
{
	[System.Serializable]
	public class ListBlueprints
	{
		public string roomListId = null;
		public List<WeightedBlueprintEntry> possibleRooms = new();
	}

	public class GenericBlueprintRegistrar : MonoBehaviour
	{
		[SerializeField] private ListBlueprints listBlueprints = new();

		public void Awake()
		{
			BluePrintSpawner.RegisterNewBlueprintList(listBlueprints.roomListId, listBlueprints.possibleRooms);
		}

		public void OnDestroy()
		{
			BluePrintSpawner.UnregisterBlueprintList(listBlueprints.roomListId);
		}
	}
}