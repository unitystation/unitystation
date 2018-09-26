using UnityEngine;
using UnityEngine.SceneManagement;

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
	private ProgressBarCrafting progressBar;
	private GameObject progressBarPrefab;

	public static Construction Construction => Instance.construction;
	public static Designs Designs => Instance.designs;
	public static Techweb TechWeb => Instance.techweb;
	public static ProgressBarCrafting ProgressBar => Instance.progressBar;

	void Start()
	{
		progressBarPrefab = Resources.Load("ProgressBarCrafting") as GameObject;
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		if (scene.name != "Lobby")
		{
			var p = PoolManager.Instance.PoolNetworkInstantiate(Instance.progressBarPrefab, Vector3.zero, Quaternion.identity);
			Instance.progressBar = p.GetComponent<ProgressBarCrafting>();
		}
	}
}