using UnityEngine;
using US13.Core.Lifecycle;
using US13.Systems.Inventory;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Items.Science.Clothing
{
	public class BagOfHolding : MonoBehaviour, IServerInventoryMove
	{
		[SerializeField] private GameObject SingularityPrefab;

		public void OnInventoryMoveServer(InventoryMove move)
		{
			if (move.ToSlot == null) return;
			move.ToSlot.ItemStorage.TryGetComponent<BagOfHolding>(out var bagOfHolding);

			if (bagOfHolding == null) return;

			Spawn.ServerPrefab(SingularityPrefab, SpawnDestination.At(bagOfHolding.GetComponent<UniversalObjectPhysics>().OfficialPosition));

			Despawn.ServerSingle(bagOfHolding.gameObject);
		}
	}
}
