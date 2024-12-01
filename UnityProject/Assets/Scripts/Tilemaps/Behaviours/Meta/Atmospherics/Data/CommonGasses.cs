using ScriptableObjects;
using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Systems.Atmospherics
{
	[CreateAssetMenu(fileName = "CommonGasses", menuName = "Atmospherics/CommonGasses")]
	public class CommonGasses : SingletonScriptableObject<CommonGasses>
	{
		public GasSO WaterVapor;
		public GasSO Oxygen;
		public GasSO CarbonDioxide;
	}
}