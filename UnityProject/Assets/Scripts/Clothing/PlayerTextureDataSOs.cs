using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerTextureDataSOs", menuName = "Singleton/PlayerTextureData")]
public class PlayerTextureDataSOs : SingletonScriptableObject<PlayerTextureDataSOs>
{
	public List<PlayerTextureData> DataRaceData = new List<PlayerTextureData>();
}
