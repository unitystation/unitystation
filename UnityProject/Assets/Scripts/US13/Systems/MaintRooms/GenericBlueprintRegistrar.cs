using System.Collections.Generic;
using SecureStuff;
using UnityEngine;
using US13.Core.Lifecycle;

namespace US13.Systems.MaintRooms
{
	[System.Serializable]
	public class ListBlueprints
	{
		public List<WeightedBlueprintEntry> possibleRooms = new();
	}

	public class GenericBlueprintRegistrar : MonoBehaviour, INewMappedOnSpawn
	{
		[SerializeField]
		private SerializableDictionary<string, ListBlueprints> listBlueprints = new();

		public void OnNewMappedOnSpawn()
		{
			foreach (var blueprint in listBlueprints)
			{
				BluePrintSpawner.RegisterNewBlueprintList(blueprint.Key, blueprint.Value.possibleRooms);
			}
		}

		public void OnDestroy()
		{
			foreach (var blueprint in listBlueprints)
			{
				BluePrintSpawner.UnregisterBlueprintList(blueprint.Key);
			}
		}
	}
}