using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Atmospherics
{
	public interface Reaction
	{
		bool Satisfies(GasMix gasMix);

		float React(ref GasMix gasMix);
	}

	public class PlasmaFireReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			return gasMix.GetMoles(Gas.Plasma) > 0 && gasMix.Temperature > Reactions.PLASMA_MINIMUM_BURN_TEMPERATURE;
		}

		public float React(ref GasMix gasMix)
		{
			float consumed = 0;

			float temperature = gasMix.Temperature;

			float temperatureScale = 1;
			if (temperature <= Reactions.PLASMA_UPPER_TEMPERATURE)
			{
				temperatureScale = (temperature - Reactions.PLASMA_MINIMUM_BURN_TEMPERATURE) /
				                   (Reactions.PLASMA_UPPER_TEMPERATURE - Reactions.PLASMA_MINIMUM_BURN_TEMPERATURE);
			}

			if (temperatureScale > 0)
			{
				float oxygenBurnRate = Reactions.OXYGEN_BURN_RATE_BASE - temperatureScale;

				// orientate plasma burn rate on the one with less moles
				float moles = Mathf.Min(gasMix.GetMoles(Gas.Plasma), gasMix.GetMoles(Gas.Oxygen) / Reactions.PLASMA_OXYGEN_FULLBURN);
				float plasmaBurnRate = (temperatureScale * moles) / Reactions.PLASMA_BURN_RATE_DELTA;

				// MINIMUM_HEAT_CAPACITY 0.0003
				if (plasmaBurnRate > 0.0003)
				{
					float heatCapacity = gasMix.HeatCapacity;

					plasmaBurnRate = Mathf.Min(plasmaBurnRate, gasMix.GetMoles(Gas.Plasma), gasMix.GetMoles(Gas.Oxygen) / oxygenBurnRate);

					float consumedOxygen = plasmaBurnRate * oxygenBurnRate;

					gasMix.RemoveGas(Gas.Plasma, plasmaBurnRate);
					gasMix.RemoveGas(Gas.Oxygen, consumedOxygen);

					gasMix.AddGas(Gas.CarbonDioxide, plasmaBurnRate);

					float energyReleased = Reactions.FIRE_PLASMA_ENERGY_RELEASED * plasmaBurnRate;

					gasMix.Temperature = (temperature * heatCapacity + energyReleased) / gasMix.HeatCapacity;

					consumed = plasmaBurnRate + consumedOxygen;
				}
			}

			return consumed;
		}
	}
}