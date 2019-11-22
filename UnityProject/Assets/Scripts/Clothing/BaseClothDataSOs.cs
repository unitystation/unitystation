using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "BaseClothDataSOs", menuName = "Singleton/BaseClothData")]
public class BaseClothDataSOs : SingletonScriptableObject<BaseClothDataSOs>
{
    public List<BaseClothData> BaseClothData = new List<BaseClothData>();
}
