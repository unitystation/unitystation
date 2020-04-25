using System;
using System.Collections.Generic;
using UnityEngine;

namespace Items
{
	[CreateAssetMenu(fileName = "RandomItemPool", menuName = "ScriptableObjects/RandomItemPool")]
	public class RandomItemPool : ScriptableObject
	{
		[Tooltip("List of game objects to choose from")][SerializeField]
		private List<GameObjectPool> pool = null;

		public List<GameObjectPool> Pool => pool;
	}

	[Serializable]
	public class GameObjectPool
	{
		[Tooltip("Object we will spawn in the world")] [SerializeField]
		private GameObject prefab = null;
		[Tooltip("Max amount we can spawn of this object")] [SerializeField]
		private int maxAmount = 1;
		[Tooltip("Probability of spawning this item when chosen")] [SerializeField] [Range(0, 100)]
		private int probability = 100;

		public GameObject Prefab => prefab;
		public int MaxAmount => maxAmount;
		public int Probability => probability;
	}
}
