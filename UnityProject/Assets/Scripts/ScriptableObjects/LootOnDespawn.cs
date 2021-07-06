using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "LootOnDespawn", menuName = "ScriptableObjects/LootOnDespawn", order = 0)]
	public class LootOnDespawn : ScriptableObject
	{
		[SerializeField] private GameObject objectToSpawn = default;
		[SerializeField] private int minAmount = default;
		[SerializeField] private int maxAmount = default;
		[SerializeField] private float minRadius = default;
		[SerializeField] private float maxRadius = default;

		//Have to use system random due to thread calling SpawnLoot
		private System.Random rnd = new System.Random();

		public void SpawnLoot(Vector3 worldPosition)
		{
			Spawn.ServerPrefab(objectToSpawn, worldPosition, count: rnd.Next(minAmount, maxAmount),
				scatterRadius: Random.Range(minRadius, maxRadius));
		}
	}
}
