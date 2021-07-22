using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "PlayerTextureData", menuName = "ScriptableObjects/PlayerTextureData", order = 1)]
public class PlayerTextureData : ScriptableObject
{
    public RaceVariantTextureData Base;
    public RaceVariantTextureData Male;
    public RaceVariantTextureData Female;
    public List<RaceVariantTextureData> Other;

    public void Awake()
    {
#if UNITY_EDITOR
        {
            if (PlayerTextureDataSOs.Instance == null)
            {
                Resources.LoadAll<PlayerTextureDataSOs>("ScriptableObjectsSingletons");
            }
            if (!PlayerTextureDataSOs.Instance.DataRaceData.Contains(this))
            {
                PlayerTextureDataSOs.Instance.DataRaceData.Add(this);
            }
        }
#endif
        InitializePool();
    }

	private void OnEnable() => SceneManager.activeSceneChanged += OnSceneLoaded;

	private void OnDisable() => SceneManager.activeSceneChanged -= OnSceneLoaded;

	private void OnSceneLoaded(Scene scene, Scene newScene) => InitializePool();

	public void InitializePool()
    {
        if (Spawn.RaceData.ContainsKey(this.name) && Spawn.RaceData[this.name] != this)
        {
            Logger.LogError($"A PlayerTextureData has a duplicate name. Name: {this.name}. " +
                "Please rename one of them.", Category.PlayerInventory);
        }
        Spawn.RaceData[this.name] = this;
    }

    public static void GetClothingDatas(List<PlayerTextureData> DataPCD)
    {
        DataPCD.Clear();
        var PCD = Resources.LoadAll<PlayerTextureData>("textures/mobs/races");
        foreach (var PCDObj in PCD)
        {
            DataPCD.Add(PCDObj);
        }
    }
}

[System.Serializable]
public class RaceVariantTextureData
{
    public SpriteDataSO Head;
    public SpriteDataSO Eyes;
    public SpriteDataSO Torso;
    public SpriteDataSO ArmRight;
    public SpriteDataSO ArmLeft;
    public SpriteDataSO HandRight;
    public SpriteDataSO HandLeft;
    public SpriteDataSO LegRight;
    public SpriteDataSO LegLeft;
}

public enum BodyPartSpriteName
{
    Null,
    Head,
    Eyes,
    Torso,
    ArmRight, ArmLeft,
    HandRight, HandLeft,
    LegRight, LegLeft,
}