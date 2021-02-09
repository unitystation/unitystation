﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Systems.Atmospherics
{
	public class PlasmaFireReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			return CanHoldHotspot(gasMix);
		}

		public static bool CanHoldHotspot(GasMix gasMix)
		{
			if (gasMix.Temperature > Reactions.PlasmaMaintainFire && gasMix.GetMoles(Gas.Plasma) > 0.1f &&
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

				float MolesPlasmaBurnt = gasMix.GetMoles(Gas.Plasma) * Reactions.BurningDelta * BurnRate;
				if (MolesPlasmaBurnt * 2 > gasMix.GetMoles(Gas.Oxygen)) {
					MolesPlasmaBurnt = (gasMix.GetMoles(Gas.Oxygen) * Reactions.BurningDelta * BurnRate)/2;
				}

				if (MolesPlasmaBurnt < 0)
				{
					return;
				}

				if (gasMix.GetMoles(Gas.Oxygen) / gasMix.GetMoles(Gas.Plasma) > AtmosDefines.SUPER_SATURATION_THRESHOLD)
				{
					superSaturated = true;
				}

				gasMix.RemoveGas(Gas.Plasma, MolesPlasmaBurnt);
				if (gasMix.Gases[Gas.Plasma] < 0) gasMix.Gases[Gas.Plasma] = 0;

				gasMix.RemoveGas(Gas.Oxygen, MolesPlasmaBurnt * 2);
				if (gasMix.Gases[Gas.Oxygen] < 0) gasMix.Gases[Gas.Oxygen] = 0;
				var TotalmolestoCO2 = MolesPlasmaBurnt + (MolesPlasmaBurnt * 2);

				if (superSaturated)
				{
					gasMix.AddGas(Gas.Tritium, TotalmolestoCO2 / 3);
				}
				else
				{
					gasMix.AddGas(Gas.CarbonDioxide, TotalmolestoCO2 / 3);
				}

				float heatCapacity = gasMix.WholeHeatCapacity;
				gasMix.SetTemperature((temperature * heatCapacity + (Reactions.EnergyPerMole * TotalmolestoCO2)) / gasMix.WholeHeatCapacity);
			}
		}

		public static float GetOxygenContact(GasMix gasMix)
		{
			float Oxygen = gasMix.GasRatio(Gas.Oxygen);
			float Plasma = gasMix.GasRatio(Gas.Plasma);

			var NeedOXtoplas = Plasma * 2;
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
