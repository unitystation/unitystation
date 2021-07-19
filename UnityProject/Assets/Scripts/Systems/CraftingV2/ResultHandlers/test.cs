using System.Collections.Generic;
using UnityEngine;

namespace Systems.CraftingV2.ResultHandlers
{
	[CreateAssetMenu(fileName = "testhandler", menuName = "ScriptableObjects/Crafting/CraftingHandlers/testhandler")]
	public class test : IResultHandler
	{
		public override void OnCraftingCompleted(
			List<GameObject> spawnedResult,
			List<CraftingIngredient> usedIngredients
		)
		{
			foreach (var res in spawnedResult)
			{
				foreach (var ing in usedIngredients)
				{
					res.Item().ServerSetArticleName(res.Item().ArticleName + ing.gameObject.Item().ArticleName + " and ");
				}
			}
		}
	}
}