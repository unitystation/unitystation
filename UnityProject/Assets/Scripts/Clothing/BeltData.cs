using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ContainerData", menuName = "ScriptableObjects/BeltData", order = 1)]
public class BeltData : BaseClothData
{
	public EquippedData sprites;

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