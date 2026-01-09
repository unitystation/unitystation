using UnityEngine;
using System;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/Item")]
	[Serializable]
	public class Item : Chemistry.Effect
	{
		private Vector3 senderPosition;
		public GameObject spawnItem;

		public int numberToSpawn = -1;

		public override void Apply(MonoBehaviour sender,ReagentMix ReagentMix, Vector3 WorldPosition, float amount)
		{
			amount = (int)Math.Floor(amount);
			if (numberToSpawn != -1)
			{
				amount = numberToSpawn;
			}

			senderPosition = WorldPosition;
			Spawn.ServerPrefab(spawnItem, senderPosition, null, null, (int)amount);
		}

		public void SpawnStuff()
		{

		}
	}
}