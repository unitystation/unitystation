using System.Collections.Generic;
using UnityEngine;
using Systems.Botany;

// TODO: refactor this and merge it into SingletonScriptableObject
public class ResourceTracker : MonoBehaviour
{
	public List<PlayerTextureData> DataRaceData = new List<PlayerTextureData>();

	public void GatherData()
	{
		PlayerTextureData.getClothingDatas(DataRaceData);
	}
}
