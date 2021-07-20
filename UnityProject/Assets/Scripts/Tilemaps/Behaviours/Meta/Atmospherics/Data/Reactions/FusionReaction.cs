using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Radiation;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class FusionReaction : Reaction
	{

		private static System.Random rnd = new System.Random();
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
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


			foreach (var gas in gasMix.GasesArray)
			{
				gasPower += gas.GasSO.FusionPower * gas.Moles;
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
				return;
			}

			gasMix.RemoveGas(Gas.Tritium, AtmosDefines.FUSION_TRITIUM_MOLES_USED);

			if (reactionEnergy > 0)
			{
				gasMix.AddGas(Gas.Oxygen, AtmosDefines.FUSION_TRITIUM_MOLES_USED * (reactionEnergy * AtmosDefines.FUSION_TRITIUM_CONVERSION_COEFFICIENT));
				gasMix.AddGas(Gas.NitrousOxide, AtmosDefines.FUSION_TRITIUM_MOLES_USED * (reactionEnergy * AtmosDefines.FUSION_TRITIUM_CONVERSION_COEFFICIENT));
			}

			if (reactionEnergy != 0)
			{
				RadiationManager.Instance.RequestPulse(node.PositionMatrix, node.Position, Mathf.Max((AtmosDefines.FUSION_RAD_COEFFICIENT/instability)+ AtmosDefines.FUSION_RAD_MAX, 0), rnd.Next(Int32.MinValue, Int32.MaxValue));

				var newHeatCap = gasMix.WholeHeatCapacity;
				if (newHeatCap > 0.0003f && (gasMix.Temperature <= AtmosDefines.FUSION_MAXIMUM_TEMPERATURE || reactionEnergy <= 0))
				{
					gasMix.SetTemperature(
						Mathf.Clamp(((gasMix.Temperature * oldHeatCap + reactionEnergy) / newHeatCap),
							AtmosDefines.SPACE_TEMPERATURE,
							Single.PositiveInfinity));
				}
			}
		}
	}
}
