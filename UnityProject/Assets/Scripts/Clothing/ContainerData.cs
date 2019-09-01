using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "ContainerData", menuName = "ScriptableObjects/BackpackData", order = 1)]
public class ContainerData : ScriptableObject
{
	public GameObject PrefabVariant;

	public EquippedData Sprites;	public StorageObjectData StorageData;
	public ItemAttributesData ItemAttributes;

	public void Awake()
	{
		InitializePool();
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}
	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		InitializePool();
	}


	public void InitializePool()
	{
		var clothFactory = Object.FindObjectOfType<ClothFactory>();
		if (clothFactory != null)
		{
			if (clothFactory.BackpackStoredData.ContainsKey(this.name))
			{
				Logger.LogError("a ContainerData Has the same name as another one. name " + this.name + ". Please rename one of them to a different name");
			}
			clothFactory.BackpackStoredData[this.name] = this;
		}

	}

}
