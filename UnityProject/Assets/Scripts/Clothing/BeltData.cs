using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ContainerData", menuName = "ScriptableObjects/BeltData", order = 1)]
public class BeltData : BaseClothData
{
	public EquippedData sprites;
	public ItemStorageStructure structure;
	public ItemStorageCapacity capacity;
	public ItemStoragePopulator populator;

	public override Sprite SpawnerIcon()
	{
		return sprites.ItemIcon.Sprites[0];
	}

	public override void InitializePool()
	{
		if (parent != null)
		{
			var parentBelt = parent as BeltData;
			if (parentBelt != null)
			{
				sprites.Combine(parentBelt.sprites);

				if (structure == null)
				{
					structure = parentBelt.structure;
				}

				if (capacity == null)
				{
					capacity = parentBelt.capacity;
				}

				if (populator == null)
				{
					populator = parentBelt.populator;
				}
			}
		}

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