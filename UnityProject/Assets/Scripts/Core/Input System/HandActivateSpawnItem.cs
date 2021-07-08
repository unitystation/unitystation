using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Items
{
	public class HandActivateSpawnItem : MonoBehaviour, IInteractable<HandActivate>
	{
		[SerializeField, FormerlySerializedAs("SeedPacket")]
		private GameObject seedPacket = default;

		[SerializeField, FormerlySerializedAs("DeleteItemOnUse")]
		private bool deleteItemOnUse = true;

		public void ServerPerformInteraction(HandActivate interaction)
		{
			var obj = Spawn.ServerPrefab(seedPacket, interaction.Performer.transform.position, parent: interaction.Performer.transform.parent).GameObject;
			var attributes = obj.GetComponent<ItemAttributesV2>();
			if (attributes != null)
			{
				Inventory.ServerAdd(obj, interaction.HandSlot, deleteItemOnUse ? ReplacementStrategy.DespawnOther : ReplacementStrategy.DropOther);
			}
			else if (deleteItemOnUse)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}
	}
}
