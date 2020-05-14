using UnityEngine;
using UnityEngine.SceneManagement;

public class CraftingManager : MonoBehaviour
{
	private static CraftingManager craftingManager;
	[SerializeField] private CraftingDatabase meals = new CraftingDatabase();
	[SerializeField] private CraftingDatabase cuts = new CraftingDatabase();
	[SerializeField] private CraftingDatabase logs = new CraftingDatabase();

	public static CraftingDatabase Meals => Instance.meals;
	public static CraftingDatabase Cuts => Instance.cuts;
	public static CraftingDatabase Logs => Instance.logs;

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

	public static Designs Designs => Instance.designs;
	public static Techweb TechWeb => Instance.techweb;
}