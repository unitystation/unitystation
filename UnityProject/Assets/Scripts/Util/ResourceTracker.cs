using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class ResourceTracker : MonoBehaviour
{
	public List<PlayerCustomisationData> DataPCD = new List<PlayerCustomisationData>();
	public List<ClothingData> DataClothingData = new List<ClothingData>();
	public List<ContainerData> DataContainerData = new List<ContainerData>();
	public List<HeadsetData> DataHeadsetData = new List<HeadsetData>();
	public List<PlayerTextureData> DataRaceData = new List<PlayerTextureData>();

	public void GatherData() {
		PlayerCustomisationData.getPlayerCustomisationDatas(DataPCD);
		ClothingData.getClothingDatas(DataClothingData);
		ContainerData.getContainerData(DataContainerData);
		HeadsetData.getHeadsetData(DataHeadsetData);
		PlayerTextureData.getClothingDatas(DataRaceData);
	}


}
