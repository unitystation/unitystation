using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "DefaultPlantDataSOs", menuName = "Singleton/DefaultPlantData")]
public class DefaultPlantDataSOs : SingletonScriptableObject<DefaultPlantDataSOs>
{
    public List<DefaultPlantData> DefaultPlantDatas = new List<DefaultPlantData>();
}

