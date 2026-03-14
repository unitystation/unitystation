using UnityEngine;

namespace US13.Items.Food
{
	/// <summary>
	/// Interface for anything that can be consumed by a player (food, drinks, deep-fried items, etc.).
	/// </summary>
	public interface IConsumable
	{
		/// <summary>
		/// Check that the eater can consume this item.
		/// </summary>
		bool CanBeConsumedBy(GameObject eater);

		/// <summary>
		/// Try to consume this item. Server side only.
		/// </summary>
		/// <param name="feeder">Player that feeds the eater. Can be the same as eater.</param>
		/// <param name="eater">Player that is going to consume the item.</param>
		/// <param name="projectileFed">Whether the item was fed via projectile.</param>
		void TryConsume(GameObject feeder, GameObject eater, bool projectileFed = false);
	}
}
