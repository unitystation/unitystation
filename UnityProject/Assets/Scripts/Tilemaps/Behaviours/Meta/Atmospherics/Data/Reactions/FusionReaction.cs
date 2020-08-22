using System;
using System.Collections;
using System.Collections.Generic;
using Radiation;
using UnityEngine;

namespace Atmospherics
{
	public class FusionReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var reactionEnergy = 0f;

			var initialPlasma = gasMix.GetMoles(Gas.Plasma);

			var initialCarbon = gasMix.GetMoles(Gas.CarbonDioxide);

			var scaleFactor = gasMix.Volume / Mathf.PI;

			var toroidalSize = (2 * Mathf.PI) + ((Mathf.PI / 180) *
			                                     Mathf.Atan((gasMix.Volume - AtmosDefines.TOROID_VOLUME_BREAKEVEN) /
			                                                AtmosDefines.TOROID_VOLUME_BREAKEVEN));

			var gasPower = 0f;

			foreach (var gas in Gas.All)
			{
				gasPower += gas.FusionPower * gasMix.GetMoles(gas);
			}

			var instability =  Mathf.Pow(gasPower * AtmosDefines.INSTABILITY_GAS_POWER_FACTOR, 2) % toroidalSize;

			var plasma = (initialPlasma - AtmosDefines.FUSION_MOLE_THRESHOLD) / scaleFactor;

			var carbon = (initialCarbon - AtmosDefines.FUSION_MOLE_THRESHOLD) / scaleFactor;

			plasma = (plasma - instability * Mathf.Sin(carbon)) % toroidalSize;

			carbon = (carbon - plasma) % toroidalSize;

			gasMix.SetGas(Gas.Plasma, plasma * scaleFactor + AtmosDefines.FUSION_MOLE_THRESHOLD);
			gasMix.SetGas(Gas.CarbonDioxide, carbon * scaleFactor + AtmosDefines.FUSION_MOLE_THRESHOLD);

			var deltaPlasma = initialPlasma - gasMix.GetMoles(Gas.Plasma);

			reactionEnergy += deltaPlasma * AtmosDefines.PLASMA_BINDING_ENERGY;

			if (instability < AtmosDefines.FUSION_INSTABILITY_ENDOTHERMALITY)
			{
				reactionEnergy = Mathf.Max(reactionEnergy, 0);
			}
			else if (reactionEnergy < 0)
			{
				reactionEnergy *= Mathf.Pow(instability - AtmosDefines.FUSION_INSTABILITY_ENDOTHERMALITY, 5);
			}

			if (gasMix.InternalEnergy + reactionEnergy < 0)
			{
				gasMix.SetGas(Gas.Plasma, initialPlasma);
				gasMix.SetGas(Gas.CarbonDioxide, initialCarbon);
				return 0f;
			}

			gasMix.RemoveGas(Gas.Tritium, AtmosDefines.FUSION_TRITIUM_MOLES_USED);

			if (reactionEnergy > 0)
			{
				gasMix.AddGas(Gas.Oxygen, AtmosDefines.FUSION_TRITIUM_MOLES_USED * (reactionEnergy * AtmosDefines.FUSION_TRITIUM_CONVERSION_COEFFICIENT));
				gasMix.AddGas(Gas.NitrousOxide, AtmosDefines.FUSION_TRITIUM_MOLES_USED * (reactionEnergy * AtmosDefines.FUSION_TRITIUM_CONVERSION_COEFFICIENT));
			}

			if (reactionEnergy != 0)
			{
				RadiationManager.Instance.RequestPulse(MatrixManager.AtPoint(tilePos.RoundToInt(), true).Matrix, tilePos.RoundToInt(), Mathf.Max((AtmosDefines.FUSION_RAD_COEFFICIENT/instability)+ AtmosDefines.FUSION_RAD_MAX, 0), UnityEngine.Random.Range(Int32.MinValue, Int32.MaxValue));

				var newHeatCap = gasMix.WholeHeatCapacity;
				if (newHeatCap > 0.0003f && (gasMix.Temperature <= AtmosDefines.FUSION_MAXIMUM_TEMPERATURE || reactionEnergy <= 0))
				{
					gasMix.Temperature = Mathf.Clamp(((gasMix.Temperature * oldHeatCap + reactionEnergy) / newHeatCap), 2.7f, Single.PositiveInfinity);
				}
			}

			return 0f;
		}
	}
}
