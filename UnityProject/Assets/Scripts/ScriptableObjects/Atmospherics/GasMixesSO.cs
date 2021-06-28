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

		[ContextMenu("Validate Gases")]
		private void Validate()
		{
			gasMix = GasMix.FromTemperature(gasMix.GasData, Reactions.KOffsetC + 20, volumeOverride);
		}

		private void OnEnable()
		{
			gasMix = GasMix.FromTemperature(gasMix.GasData, Reactions.KOffsetC + 20, volumeOverride);
		}
	}
}
