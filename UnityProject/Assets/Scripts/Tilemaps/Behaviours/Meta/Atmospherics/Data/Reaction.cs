using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

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
			if (gasMix.Temperature > Reactions.PlasmaMaintainFire && gasMix.GetMoles(Gas.Plasma) > 0 &&
	gasMix.GetMoles(Gas.Oxygen) > 0)
			{
				if (GetOxygenContact(gasMix) > Reactions.MinimumOxygenContact)
				{
					return (true);
				}
				else {
					return (false);
				}
			}
			else {
				return (false);
			}
		}

		public float React(ref GasMix gasMix)
		{
			float consumed = 0;

			float temperature = gasMix.Temperature;

			float BurnRate = GetOxygenContact(gasMix);
			//Logger.Log(BurnRate.ToString() + "BurnRate");
			if (BurnRate > 0)
			{
				float MolesPlasmaBurnt = gasMix.GetMoles(Gas.Plasma) * Reactions.BurningDelta * BurnRate;
				if (MolesPlasmaBurnt * 2 > gasMix.GetMoles(Gas.Oxygen)) {
					MolesPlasmaBurnt = (gasMix.GetMoles(Gas.Oxygen) * Reactions.BurningDelta * BurnRate)/2;
				}
				gasMix.RemoveGas(Gas.Plasma, MolesPlasmaBurnt);
				gasMix.RemoveGas(Gas.Oxygen, MolesPlasmaBurnt * 2);
				var TotalmolestoCO2 = MolesPlasmaBurnt + (MolesPlasmaBurnt * 2);
				gasMix.AddGas(Gas.CarbonDioxide, TotalmolestoCO2 / 3);

				float heatCapacity = gasMix.WholeHeatCapacity;
				gasMix.Temperature = (temperature * heatCapacity + (Reactions.EnergyPerMole * TotalmolestoCO2)) / gasMix.WholeHeatCapacity;
				consumed = TotalmolestoCO2;
			}
			return (consumed);
		}

		public static float GetOxygenContact(GasMix gasMix)
		{
			float Oxygen = gasMix.GasRatio(Gas.Oxygen);
			float Plasma = gasMix.GasRatio(Gas.Plasma);

			var NeedOXtoplas = Plasma * 2;
			var Ratio = 0.0f;
			if (Oxygen > NeedOXtoplas)
			{
				Ratio = NeedOXtoplas / Oxygen;
			}
			else {
				Ratio = Oxygen / NeedOXtoplas;
			}
			var tempComponent = 0.0f;
			var temp = gasMix.Temperature;
			if (temp < Reactions.PlasmaMaintainFire)
			{
				tempComponent = 0;
			}
			else if (temp >= Reactions.PlasmaMaxTemperatureGain)
			{
				tempComponent = 1;
			}
			else {
				tempComponent = (float)Math.Pow((temp - Reactions.PlasmaMaintainFire) /
				(Reactions.PlasmaMaxTemperatureGain - Reactions.PlasmaMaintainFire), 2);
			}

			//Logger.Log("Ratio >" + Ratio + " ((Oxygen + Plasma / gasMix.Moles) that suff" + (Oxygen + Plasma / gasMix.Moles) + " (tempComponent * 3) >" +(1 + tempComponent * 2));
			return (Ratio * ((Oxygen + Plasma / gasMix.Moles) * (1+tempComponent * 2)));
		}
	}
}