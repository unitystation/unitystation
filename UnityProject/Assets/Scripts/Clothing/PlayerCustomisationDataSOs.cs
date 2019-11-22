using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerCustomisationDataSOs", menuName = "Singleton/PlayerCustomisationData")]
public class PlayerCustomisationDataSOs : SingletonScriptableObject<PlayerCustomisationDataSOs>
{
    public List<PlayerCustomisationData> DataPCD = new List<PlayerCustomisationData>();
}
