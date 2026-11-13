using System.Collections.Generic;
using UnityEngine;
using US13.Core.Attributes;
using US13.Core.Lifecycle;
using US13.Systems.Inventory;
using Util;

namespace US13.Items.InventoryMoveEffects
{
	/// <summary>
	/// Bridge between <see cref="Pickupable"/> inventory moves and <see cref="IInventoryMoveEffect"/>.
	/// Applies the configured effect when the item is moved into one of the configured slots.
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	public class EffectOnInventoryMove : MonoBehaviour, IServerLifecycle
	{
		[Tooltip("The item applies the effect when moved into any of these slots (e.g. head).")]
		[SerializeField]
		private List<NamedSlot> slots = new();

		[SerializeReference]
		[SelectImplementation(typeof(IInventoryMoveEffect))]
		private IInventoryMoveEffect effect;

		private Pickupable pickupable;

		public void OnSpawnServer(SpawnInfo info)
		{
			pickupable = this.GetCachedComponent<Pickupable>();
			pickupable.OnInventoryMoveServerEvent.AddListener(OnInventoryMove);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			pickupable.OnInventoryMoveServerEvent.RemoveListener(OnInventoryMove);
		}

		private void OnInventoryMove(GameObject item)
		{
			var slot = pickupable.ItemSlot;
			if (slot == null || effect == null) return;

			var namedSlot = slot.NamedSlot;
			if (namedSlot.HasValue == false || slots.Contains(namedSlot.Value) == false) return;

			var player = slot.RootPlayer();
			if (player == null) return;

			effect.Apply(item, player);
		}
	}
}