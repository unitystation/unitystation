using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ContainerData", menuName = "ScriptableObjects/BeltData", order = 1)]
public class BeltData : BaseClothData
{
	public EquippedData sprites;

	public override Sprite SpawnerIcon()
	{
		return sprites.ItemIcon.Sprites[0];
	}

	public override void InitializePool()
	{
		if (Spawn.BeltStoredData.ContainsKey(name) && Spawn.BeltStoredData[name] != this)
		{
			Logger.LogError("a BeltData has the same name as another one; name " + name + ". Please rename one of them to a different name");
			return;
		}

		Spawn.BeltStoredData[name] = this;
	}

	public static void GetBeltData(List<BeltData> dataPcd)
	{
		dataPcd.Clear();

		var pcd = Resources.LoadAll<BeltData>("textures/clothing");

		foreach(var data in pcd)
		{
			dataPcd.Add(data);
		}
	}
}