using System.Collections.Generic;
using UnityEngine;
using Systems.Botany;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "DefaultPlantDataSOs", menuName = "Singleton/DefaultPlantData")]
	public class DefaultPlantDataSOs : SingletonScriptableObject<DefaultPlantDataSOs>
	{
		public List<DefaultPlantData> DefaultPlantDatas = new List<DefaultPlantData>();
	}
}
