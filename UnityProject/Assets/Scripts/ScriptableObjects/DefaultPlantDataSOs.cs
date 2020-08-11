using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "DefaultPlantDataSOs", menuName = "Singleton/DefaultPlantData")]
	public class DefaultPlantDataSOs : SingletonScriptableObject<DefaultPlantDataSOs>
	{
		public List<DefaultPlantData> DefaultPlantDatas = new List<DefaultPlantData>();
	}
}

