using System.Collections.Generic;
using UnityEngine;

// TODO: refactor this and merge it into SingletonScriptableObject
public class ResourceTracker : MonoBehaviour
{
	public List<PlayerTextureData> DataRaceData = new List<PlayerTextureData>();
	public List<DefaultPlantData> DataDefaultPlantData = new List<DefaultPlantData>();

	public void GatherData()
	{
		PlayerTextureData.getClothingDatas(DataRaceData);
		DefaultPlantData.getDatas(DataDefaultPlantData);
	}
}