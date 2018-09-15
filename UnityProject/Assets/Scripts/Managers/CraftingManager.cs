using UnityEngine;

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

	public Techweb techweb;
	public Designs designs;
	public Construction construction;

	public static Construction Construction => Instance.construction;
	public static Designs Designs => Instance.designs;
	public static Techweb TechWeb => Instance.techweb;
}