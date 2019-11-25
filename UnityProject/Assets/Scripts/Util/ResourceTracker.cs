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
	public List<ClothingData> DataClothingData = new List<ClothingData>();
	public List<ContainerData> DataContainerData = new List<ContainerData>();
	public List<BeltData> DataBeltData = new List<BeltData>();
	public List<HeadsetData> DataHeadsetData = new List<HeadsetData>();
	public List<PlayerTextureData> DataRaceData = new List<PlayerTextureData>();
	public List<DefaultPlantData> DataDefaultPlantData = new List<DefaultPlantData>();

	public void GatherData()
	{
		PlayerCustomisationData.getPlayerCustomisationDatas(DataPCD);
		ClothingData.getClothingDatas(DataClothingData);
		ContainerData.getContainerData(DataContainerData);
		BeltData.GetBeltData(DataBeltData);
		HeadsetData.getHeadsetData(DataHeadsetData);
		PlayerTextureData.getClothingDatas(DataRaceData);
		DefaultPlantData.getDatas(DataDefaultPlantData);
	}
}