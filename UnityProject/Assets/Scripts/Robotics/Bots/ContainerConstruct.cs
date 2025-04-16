using System;
using UnityEngine;
using Chemistry.Components;
using Util;

namespace Items.Construction
{
	public class ContainerConstruct : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[Tooltip("The possible items that can be added to this construction")]
		[SerializeField] private GameObject[] craftItem;

		[Tooltip("The amount of the required items to be applied")]
		[SerializeField] private int amount = 1;

		[Tooltip("The prefab that will spawn once item is given")]
		[SerializeField] private GameObject prefabToSpawn;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			var hand = interaction.HandObject != null ? interaction.HandObject : null;
			if (hand == null) return false;

			foreach (var neededObject in craftItem)
			{
				if (hand.GetComponent<PrefabTracker>()?.ForeverID == neededObject.GetComponent<PrefabTracker>().ForeverID)
				{
					return true;
				}
			}
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.UsedObject.TryGetComponent<Stackable>(out var stack) == true)
			{
				int toConsume = Math.Min(amount, stack.Amount);

				stack.ServerConsume(toConsume);
				amount -= toConsume;
			}
			else
			{
				amount--;
				_ = Inventory.ServerDespawn(interaction.HandObject);
			}

			if (amount > 0) return;
			Spawn.ServerPrefab(prefabToSpawn, gameObject.RegisterTile().WorldPosition, transform.parent, count: 1);
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
