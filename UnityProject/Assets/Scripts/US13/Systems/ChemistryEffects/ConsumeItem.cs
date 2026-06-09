using System;
using Chemistry;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Items.Others;
using US13.Systems.Inventory;
using Util;

namespace US13.Systems.ChemistryEffects
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/ConsumeItem")]
	public class ConsumeItem : Chemistry.Effect
	{
		private MixingBowl senderInfo;
		private Vector3Int senderPosition;
		public GameObject spawnItem;
		public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix, Vector3 WorldPosition, float amount)
		{
			if (sender == null) return;
			amount = (int)Math.Floor(amount);
			senderPosition = sender.gameObject.RegisterTile().WorldPositionServer;
			senderInfo = sender.gameObject.GetComponent<MixingBowl>();
			if (senderInfo != null)
			{
				if (senderInfo.playerHolding != null)
				{
					var spawnInstance = Spawn.ServerPrefab(spawnItem).GameObject;
					var pickupable = spawnInstance.GetComponent<Pickupable>();
					Inventory.Inventory.ServerAdd(pickupable, senderInfo.currentSlot, ReplacementStrategy.DespawnOther);
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