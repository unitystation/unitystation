using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LavaLandRandomArea", menuName = "ScriptableObjects/LavaLandRandomArea")]
public class LavaLandRandomAreaSO : ScriptableObject
{
	public LavaLandData[] AreaPrefabData;
}

[System.Serializable]
public class LavaLandData
{
	public GameObject AreaPrefab;
	public AreaSizes AreaSize;
	public bool SpawnOnceOnly;
}

public enum AreaSizes
{
	FiveByFive,
	TenByTen,
	FifteenByFifteen,
	TwentyByTwenty,
	TwentyfiveByTwentyfive
}