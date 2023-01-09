using UnityEngine;

public class BagOfHolding : MonoBehaviour, IServerInventoryMove
{
	[SerializeField] private GameObject SingularityPrefab;

	public void OnInventoryMoveServer(InventoryMove move)
	{
		move.ToSlot.ItemStorage.TryGetComponent<BagOfHolding>(out var bagOfHolding);

		if (bagOfHolding == null) return;

		Spawn.ServerPrefab(SingularityPrefab, SpawnDestination.At(bagOfHolding.GetComponent<UniversalObjectPhysics>().OfficialPosition));

		Despawn.ServerSingle(bagOfHolding.gameObject);
	}
}
