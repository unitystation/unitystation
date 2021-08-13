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

		/// <summary>
		/// Do not change anything directly, copy this gas mix using gasMix.Copy() or GasMix.NewGasMix()
		/// As this is an SO and will change the original values
		/// </summary>
		public GasMix BaseGasMix => gasMix;

		[SerializeField]
		private float volumeOverride = AtmosConstants.TileVolume;

		[ContextMenu("Validate Gases")]
		private void Validate()
		{
			gasMix = GasMix.FromTemperature(gasMix.GasData, Reactions.KOffsetC + 20, volumeOverride);
		}

		//Commented out as it might be causing issues where gases are lost
		//When making a new gas mix use the above menu button to set the gas values correctly
		//TODO try to fix
		// private void OnEnable()
		// {
		// 	gasMix = GasMix.FromTemperature(gasMix.GasData, Reactions.KOffsetC + 20, volumeOverride);
		// }
	}
}
