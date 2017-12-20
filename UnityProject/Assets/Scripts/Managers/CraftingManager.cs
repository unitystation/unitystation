using UnityEngine;

namespace Crafting
{
	public class CraftingManager : MonoBehaviour
	{
		private static CraftingManager craftingManager;
		[SerializeField] private CraftingDatabase meals = new CraftingDatabase();

		public static CraftingDatabase Meals => Instance.meals;

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
	}
}