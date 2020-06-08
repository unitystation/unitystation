using UnityEngine;
using System;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/Item")]
	[Serializable]
	public class Item : Chemistry.Effect
	{
		public GameObject spawnItem;
		public override void Apply(MonoBehaviour sender, float amount)
		{
			amount = (int)Math.Floor(amount);
			Spawn.ServerPrefab(spawnItem, sender.gameObject.RegisterTile().WorldPositionServer, null, null, (int)amount);
		}
	}
}