using System;
using System.Linq;
using Systems.Atmospherics;
using UnityEngine;

namespace ScriptableObjects.Atmospherics
{
	[CreateAssetMenu(fileName = "GasMixesSO", menuName = "ScriptableObjects/Atmos/GasMixesSO")]
	public class GasMixesSO : ScriptableObject
	{
		[SerializeField]
		private GasMix gasMix = new GasMix();
		public GasMix GasMix => gasMix;

		[SerializeField]
		private float volumeOverride = AtmosConstants.TileVolume;

		private void OnValidate()
		{
			if(gasMix.GasData.GasesArray.Any(x => x.GasSO == null)) return;

			gasMix = GasMix.FromTemperature(gasMix.GasData, Reactions.KOffsetC + 20, volumeOverride);
		}
	}
}
