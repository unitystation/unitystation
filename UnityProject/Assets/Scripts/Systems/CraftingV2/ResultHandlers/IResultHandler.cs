using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.CraftingV2.ResultHandlers
{
	/// <summary>
	/// 	The class whose heirs can handle specified craft actions. For example, we can set a spear's damage
	/// 	according to a glass shard used for crafting.
	/// </summary>
	[Serializable]
	public class IResultHandler : ScriptableObject
	{
		/// <summary>
		/// 	Called when a result is already spawned and ingredients already used.
		/// </summary>
		/// <param name="spawnedResult">The spawned result after a successful crafting action.</param>
		/// <param name="usedIngredients">The used ingredients after a successful crafting action.</param>
		/// <exception cref="NotImplementedException"></exception>
		public virtual void OnCraftingCompleted(
			List<GameObject> spawnedResult,
			List<CraftingIngredient> usedIngredients
		)
		{
			throw new NotImplementedException();
		}
	}
}