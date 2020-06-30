using UnityEngine;
using System;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/Item")]
	[Serializable]
	public class Item : Chemistry.Effect
	{
		public RegisterPlayer playerHolding;
		public GameObject spawnItem;
		public override void Apply(MonoBehaviour sender, float amount)
		{
			amount = (int)Math.Floor(amount);
			if (sender.gameObject.GetComponent<MixingBowl>() != null)
			{
				playerHolding = sender.gameObject.GetComponent<MixingBowl>().playerHolding;
				if (playerHolding != null)
					Spawn.ServerPrefab(spawnItem, playerHolding.WorldPositionServer, null, null, (int)amount);
				else
					Spawn.ServerPrefab(spawnItem, sender.gameObject.RegisterTile().WorldPositionServer, null, null, (int)amount);
			}
			else
			{
				Spawn.ServerPrefab(spawnItem, sender.gameObject.RegisterTile().WorldPositionServer, null, null, (int)amount);
			}
		}
	}
}