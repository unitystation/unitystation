using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;

public class HandActivateSpawnItem : MonoBehaviour, IInteractable<HandActivate>
{
	public GameObject SeedPacket;
	public bool DeleteItemOnUse = true;
	public void ServerPerformInteraction(HandActivate interaction)
	{
		var _Object = Spawn.ServerPrefab(SeedPacket, interaction.Performer.transform.position, parent: interaction.Performer.transform.parent).GameObject;
		var Attributes = _Object.GetComponent<ItemAttributesV2>();
		if (Attributes != null)
		{
			var slot = interaction.HandSlot;
			if (DeleteItemOnUse)
			{
				Inventory.ServerAdd(_Object, interaction.HandSlot, ReplacementStrategy.DespawnOther);
			}
			else {
				Inventory.ServerAdd(_Object, interaction.HandSlot, ReplacementStrategy.DropOther);
			}
		}
		else {
			if (DeleteItemOnUse)
			{
				Despawn.ServerSingle(this.gameObject);
			}
		}
	}
}
