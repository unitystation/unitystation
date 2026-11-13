using System;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Items.InventoryMoveEffects
{
	/// <summary>
	/// Spills all reagents of the item's <see cref="ReagentContainer"/> onto the holder.
	/// </summary>
	[Serializable]
	public class SpillReagentsEffect : IInventoryMoveEffect
	{
		public void Apply(GameObject item, RegisterPlayer holder)
		{
			var container = item.GetCachedComponent<ReagentContainer>();
			if (container == null || container.IsEmpty) return;

			container.Spill(holder.WorldPositionServer, container.Total);
		}
	}
}
