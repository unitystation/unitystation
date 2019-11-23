
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Base class for the different types of cloth data.
/// </summary>
public abstract class BaseClothData : ScriptableObject
{
	/// <summary>
	/// Prefab variant to use to spawn this cloth rather than the default
	/// for this cloth type.
	/// </summary>
	public GameObject PrefabVariant;
	/// <summary>
	/// Various attributes of this cloth
	/// </summary>
	public ItemAttributesData ItemAttributes;


	[Tooltip("The values to pull from if this SO does not define them. Numbers will override always unless set to -1.")]
	public BaseClothData parent; // Defines the parent to use values from if values are missing


	/// <summary>
	/// Return the sprite to show for this in the dev spawner
	/// </summary>
	/// <returns></returns>
	public abstract Sprite SpawnerIcon();

	private void Awake()
	{

#if UNITY_EDITOR
		{
			if (BaseClothDataSOs.Instance == null)
			{
				Resources.LoadAll<BaseClothDataSOs>("ScriptableObjects/SOs singletons");
			}
			if (!BaseClothDataSOs.Instance.BaseClothData.Contains(this))
			{
				BaseClothDataSOs.Instance.BaseClothData.Add(this);
			}

		}

#endif

		if (parent != null)
		{
			ItemAttributes.Combine(parent.ItemAttributes);
		}

		InitializePool();
	}

	private void OnEnable()
	{
		//Logger.Log(name + " OnEnable");
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (parent != null)
		{
			ItemAttributes.Combine(parent.ItemAttributes);
		}

		InitializePool();
	}

	public abstract void InitializePool();


}
