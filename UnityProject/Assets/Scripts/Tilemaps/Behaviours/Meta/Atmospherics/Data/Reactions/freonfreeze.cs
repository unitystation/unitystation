using System.Collections;
using System.Collections.Generic;
using Systems.Atmospherics;
using UnityEngine;
using Reaction = Chemistry.Reaction;

public class freonfreeze : Systems.Atmospherics.Reaction
{
	public bool Satisfies(GasMix gasMix)
	{
		throw new System.NotImplementedException();
	}

	public void React(GasMix gasMix, MetaDataNode node)
	{
		gasMix.SetTemperature(TemperatureUtils.ZERO_CELSIUS_IN_KELVIN - 50f);
	}
}