using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Systems.Atmospherics
{
	public class StyreneFireReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			return CanHoldHotspot(gasMix);
		}

		public static bool CanHoldHotspot(GasMix gasMix)
		{
			if (gasMix.Temperature > Reactions.StyreneMaintainFire && gasMix.GetMoles(Gas.Styrene) > 0.1f &&
			    gasMix.GetMoles(Gas.Oxygen) > 0.1f)
			{
				if (GetOxygenContact(gasMix) > Reactions.MinimumOxygenContact)
				{
					return true;
				}
			}
			return false;
		}

		public void React(GasMix gasMix, Vector3 tilePos, Matrix matrix)
		{
			float temperature = gasMix.Temperature;

			float BurnRate = GetOxygenContact(gasMix);
			//Logger.Log(BurnRate.ToString() + "BurnRate");
			if (BurnRate > 0)
			{
				var superSaturated = false;

				float MolesStyreneBurnt = gasMix.GetMoles(Gas.Styrene) * Reactions.BurningDelta * BurnRate;
				if (MolesStyreneBurnt * 2 > gasMix.GetMoles(Gas.Oxygen)) {
					MolesStyreneBurnt = (gasMix.GetMoles(Gas.Oxygen) * Reactions.BurningDelta * BurnRate)/2;
				}

				if (MolesStyreneBurnt < 0)
				{
					return;
				}

				gasMix.RemoveGas(Gas.Styrene, MolesStyreneBurnt);
				if (gasMix.Gases[Gas.Styrene] < 0) gasMix.Gases[Gas.Styrene] = 0;

				gasMix.RemoveGas(Gas.Oxygen, MolesStyreneBurnt * 2);
				if (gasMix.Gases[Gas.Oxygen] < 0) gasMix.Gases[Gas.Oxygen] = 0;
				var TotalmolestoCO2 = MolesStyreneBurnt + (MolesStyreneBurnt * 2);

				gasMix.AddGas(Gas.CarbonDioxide, TotalmolestoCO2);

				float heatCapacity = gasMix.WholeHeatCapacity;
				gasMix.SetTemperature((temperature * heatCapacity + (Reactions.EnergyPerMole * TotalmolestoCO2)) / gasMix.WholeHeatCapacity);
			}
		}

		public static float GetOxygenContact(GasMix gasMix)
		{
			float Oxygen = gasMix.GasRatio(Gas.Oxygen);
			float Styrene = gasMix.GasRatio(Gas.Styrene);

			var NeedOXtoplas = Styrene * 2;
			var Ratio = 0.0f;
			if (Oxygen > NeedOXtoplas)
			{

				Ratio = 1;
			}
			else {
				Ratio = Oxygen / NeedOXtoplas;
			}
			var tempComponent = 0.0f;
			var temp = gasMix.Temperature;
			if (temp < Reactions.StyreneMaintainFire)
			{
				tempComponent = 0;
			}
			else if (temp >= Reactions.StyreneMaxTemperatureGain)
			{
				tempComponent = 1;
			}
			else {
				tempComponent = (float)Math.Pow((temp - Reactions.StyreneMaintainFire) /
				(Reactions.StyreneMaxTemperatureGain - Reactions.StyreneMaintainFire), 2);
			}

			return (Ratio * ((Oxygen + Styrene / gasMix.Moles) * (1+tempComponent * 2)));
		}
	}
}
