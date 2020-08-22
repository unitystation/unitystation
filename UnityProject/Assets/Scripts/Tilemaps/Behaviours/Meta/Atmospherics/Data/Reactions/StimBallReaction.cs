using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public class StimBallReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var ballShotAngle = 180 * Mathf.Cos(gasMix.GetMoles(Gas.WaterVapor) * gasMix.GetMoles(Gas.Nitryl)) + 180;

			var stimUsed = Mathf.Min(AtmosDefines.STIM_BALL_GAS_AMOUNT * gasMix.GetMoles(Gas.Plasma), gasMix.GetMoles(Gas.Stimulum));

			var pluoxUsed = Mathf.Min(AtmosDefines.STIM_BALL_GAS_AMOUNT * gasMix.GetMoles(Gas.Plasma), gasMix.GetMoles(Gas.Pluoxium));

			var energyReleased = stimUsed * AtmosDefines.STIMULUM_HEAT_SCALE;

			//TODO shoot stim projectile using the angle

			gasMix.AddGas(Gas.CarbonDioxide, 4 * pluoxUsed);
			gasMix.AddGas(Gas.Nitrogen, 8 * stimUsed);

			gasMix.SetGas(Gas.Plasma, gasMix.GetMoles(Gas.Plasma) * 0.5f);

			gasMix.RemoveGas(Gas.Pluoxium, 10 * pluoxUsed);
			gasMix.RemoveGas(Gas.Stimulum, 20 * stimUsed);

			gasMix.Temperature = Mathf.Clamp((gasMix.Temperature * oldHeatCap + energyReleased)/gasMix.WholeHeatCapacity, 2.7f, Single.PositiveInfinity);

			return 0f;
		}
	}
}