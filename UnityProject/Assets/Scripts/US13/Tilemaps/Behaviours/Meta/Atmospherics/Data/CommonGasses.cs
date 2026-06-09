using UnityEngine;
using US13.ScriptableObjects;
using US13.ScriptableObjects.Atmospherics;

namespace US13.Tilemaps.Behaviours.Meta.Atmospherics.Data
{
	[CreateAssetMenu(fileName = "CommonGasses", menuName = "Atmospherics/CommonGasses")]
	public class CommonGasses : SingletonScriptableObject<CommonGasses>
	{
		public GasSO WaterVapor;
		public GasSO Oxygen;
		public GasSO CarbonDioxide;
	}
}