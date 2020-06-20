using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineStarter : ReactorChamberRod
{

	public float NeutronGenerationPerSecond = 4;

	public override RodType GetRodType()
	{
		return (RodType.Fuel);
	}
}
