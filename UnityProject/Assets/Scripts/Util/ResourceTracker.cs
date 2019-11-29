using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class ResourceTracker : MonoBehaviour
{
	public List<PlayerCustomisationData> DataPCD = new List<PlayerCustomisationData>();
	public List<PlayerTextureData> DataRaceData = new List<PlayerTextureData>();
	public List<DefaultPlantData> DataDefaultPlantData = new List<DefaultPlantData>();

	public void GatherData()
	{
		PlayerCustomisationData.getPlayerCustomisationDatas(DataPCD);
		PlayerTextureData.getClothingDatas(DataRaceData);
		DefaultPlantData.getDatas(DataDefaultPlantData);
	}
}