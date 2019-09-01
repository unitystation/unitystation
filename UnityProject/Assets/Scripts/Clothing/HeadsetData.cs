using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "HeadsetData", menuName = "ScriptableObjects/HeadsetData", order = 2)]
public class HeadsetData : ScriptableObject
{
	public GameObject PrefabVariant;
	public EquippedData Sprites;
	public ItemAttributesData ItemAttributes;
	public HeadsetKyes Key;

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
			if (clothFactory.HeadSetStoredData.ContainsKey(this.name))
			{
				Logger.LogError("a HeadsetData Has the same name as another one. name " + this.name + ". Please rename one of them to a different name");
			}
			clothFactory.HeadSetStoredData[this.name] = this;
		}

	}
}


[System.Serializable]
public class HeadsetKyes
{
	public EncryptionKeyType EncryptionKey;
}