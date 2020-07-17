using UnityEngine;

namespace Container
{
	[CreateAssetMenu(fileName = "LootOnDespawn", menuName = "ScriptableObjects/LootOnDespawn", order = 0)]
	public class LootOnDespawn : ScriptableObject
	{
		[SerializeField] private GameObject objectToSpawn;
		[SerializeField] private int minAmount;
		[SerializeField] private int maxAmount;
		[SerializeField] private float minRadius;
		[SerializeField] private float maxRadius;

		public void SpawnLoot(Vector3 worldPosition)
		{
			Spawn.ServerPrefab(objectToSpawn, worldPosition, count: Random.Range(minAmount, maxAmount),
				scatterRadius: Random.Range(minRadius, maxRadius));
		}
	}
}