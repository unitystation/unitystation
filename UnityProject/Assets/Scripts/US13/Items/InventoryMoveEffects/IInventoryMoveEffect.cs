using UnityEngine;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Items.InventoryMoveEffects
{
	/// <summary>
	/// Polymorphic effect applied when an item is moved into one of the configured inventory slots.
	/// Implementations must be <c>[Serializable]</c> for <c>[SerializeReference]</c> usage in
	/// <see cref="EffectOnInventoryMove"/>.
	/// </summary>
	public interface IInventoryMoveEffect
	{
		/// <summary>
		/// Applies the effect. Server side only.
		/// </summary>
		/// <param name="item">The item that was moved.</param>
		/// <param name="holder">The player whose inventory the item was moved into.</param>
		void Apply(GameObject item, RegisterPlayer holder);
	}
}
