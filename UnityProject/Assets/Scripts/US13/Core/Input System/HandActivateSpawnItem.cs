using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Addressables.Types;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Items;
using US13.Managers;
using US13.Systems.Inventory;

namespace US13.Core.Input_System
{
	public class HandActivateSpawnItem : MonoBehaviour, IInteractable<HandActivate>
	{
		[SerializeField, FormerlySerializedAs("SeedPacket")]
		private GameObject seedPacket = default;

		[SerializeField, FormerlySerializedAs("DeleteItemOnUse")]
		private bool deleteItemOnUse = true;

		[SerializeField]
		private AddressableAudioSource spawnSound = default;

		public void ServerPerformInteraction(HandActivate interaction)
		{
			SoundManager.PlayNetworkedAtPos(spawnSound, interaction.Performer.transform.position, sourceObj: interaction.Performer);

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
