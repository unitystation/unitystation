using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.CraftingV2.ResultHandlers
{
	[Serializable]
	public class IResultHandler : ScriptableObject
	{
		public virtual void OnCraftingCompleted(
			List<GameObject> spawnedResult,
			List<CraftingIngredient> usedIngredients
		)
		{
			throw new NotImplementedException();
		}
	}
}