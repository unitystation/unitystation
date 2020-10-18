using UnityEngine;
using System;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/ConsumeItem")]
	public class ConsumeItem : Chemistry.Effect
	{
		private MixingBowl senderInfo;
		private Vector3Int senderPosition;
		public GameObject spawnItem;
		public override void Apply(MonoBehaviour sender, float amount)
		{
			amount = (int)Math.Floor(amount);
			senderPosition = sender.gameObject.RegisterTile().WorldPositionServer;
			senderInfo = sender.gameObject.GetComponent<MixingBowl>();
			if (senderInfo != null)
			{
				if (senderInfo.playerHolding != null)
				{
					var spawnInstance = Spawn.ServerPrefab(spawnItem).GameObject;
					var pickupable = spawnInstance.GetComponent<Pickupable>();
					Inventory.ServerAdd(pickupable, senderInfo.currentSlot, ReplacementStrategy.DespawnOther);
				}
				else
				{
					Spawn.ServerPrefab(spawnItem, senderPosition, null, null, (int)amount);
					Despawn.ServerSingle(sender.gameObject);
				}
			}
			else
			{
				Spawn.ServerPrefab(spawnItem, senderPosition, null, null, (int)amount);
				Despawn.ServerSingle(sender.gameObject);
			}
		}
	}
}