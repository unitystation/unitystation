
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

	private void Awake()
	{
		InitializePool();
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		InitializePool();
	}

	public abstract void InitializePool();


}
