using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
	private static CraftingManager craftingManager;
	[SerializeField] private CraftingDatabase meals = new CraftingDatabase();
	[SerializeField] private CraftingDatabase cuts = new CraftingDatabase();
	[SerializeField] private CraftingDatabase logs = new CraftingDatabase();
	[SerializeField] private CraftingDatabase roll = new CraftingDatabase();
	[SerializeField] private CraftingDatabase simplemeal = new CraftingDatabase();
	[SerializeField] private GrinderDatabase grind = new GrinderDatabase();
	[SerializeField] private CraftingDatabase mix = new CraftingDatabase();

	public static CraftingDatabase Meals => Instance.meals;
	public static CraftingDatabase Cuts => Instance.cuts;
	public static CraftingDatabase Logs => Instance.logs;
	public static CraftingDatabase Roll => Instance.roll;
	public static CraftingDatabase SimpleMeal => Instance.simplemeal;
	public static GrinderDatabase Grind => Instance.grind;
	public static CraftingDatabase Mix => Instance.mix;

	public static CraftingManager Instance
	{
		get
		{
			if (!craftingManager)
			{
				craftingManager = FindObjectOfType<CraftingManager>();
			}

			return craftingManager;
		}
	}

	private static GameObject Craft(List<ItemAttributesV2> ingredients, CraftingDatabase databaseToCheck, out List<ItemAttributesV2> remains)
	{
		Recipe recipe = databaseToCheck.FindRecipeFromIngredients(ingredients);
		if (recipe != null)
		{
			SpawnResult spwn = Spawn.ServerPrefab(recipe.Output, SpawnDestination.At(), recipe.OutputAmount);

			if (spwn.Successful)
			{
				recipe.Consume(ingredients, out remains);
				return spwn.GameObject;
			}
		}

		remains = new List<ItemAttributesV2>(ingredients);
		return null;
	}

	public static bool MergeInteraction(InventoryApply iApply, CraftingDatabase databaseToCheck)
	{
		ItemAttributesV2 toSlot = iApply.TargetObject.GetComponent<ItemAttributesV2>();
		ItemAttributesV2 fromSlot = iApply.UsedObject.GetComponent<ItemAttributesV2>();

		List<ItemAttributesV2> ingredients = new List<ItemAttributesV2>() { toSlot, fromSlot };
		GameObject result = Craft(ingredients, databaseToCheck, out List<ItemAttributesV2> remains);
		if (result != null)
		{
			if (!remains.Contains(toSlot))
			{
				Inventory.ServerAdd(result, iApply.TargetSlot);
			}
			else if (!remains.Contains(fromSlot))
			{
				Inventory.ServerAdd(result, iApply.FromSlot);
			}
			return true;
		}
		return false;
	}

	public static bool InventoryApplyInteraction(InventoryApply iApply, CraftingDatabase databaseToCheck)
	{
		ItemAttributesV2 toSlot = iApply.TargetObject.GetComponent<ItemAttributesV2>();

		List<ItemAttributesV2> ingredients = new List<ItemAttributesV2>() { toSlot };
		GameObject result = Craft(ingredients, databaseToCheck, out List<ItemAttributesV2> remains);
		if (result != null)
		{
			if (!remains.Contains(toSlot))
			{
				Inventory.ServerAdd(result, iApply.TargetSlot);
			}
			return true;
		}
		return false;
	}

	public Techweb techweb;
	public Designs designs;

	public static Designs Designs => Instance.designs;
	public static Techweb TechWeb => Instance.techweb;
}