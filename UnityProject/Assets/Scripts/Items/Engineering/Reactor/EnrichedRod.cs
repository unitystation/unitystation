using System;
using System.Collections;
using System.Collections.Generic;
using Items.Engineering;
using Systems.Explosions;
using UnityEngine;


public class EnrichedRod : FuelRod
{
	public override decimal PresentAtoms { get; set; }  = 600000000000000000;
	public override decimal fuelNeutronGeneration { get; set; }  = -2M;
	public override decimal PresentAtomsfuel { get; set; }  = 600000000000000000;

	public decimal NeutronSingularity = 76488300000M;

	public override (decimal newEnergy, decimal newNeutrons, bool Break) ProcessRodHit(decimal AbsorbedNeutrons)
	{
		var data = base.ProcessRodHit(AbsorbedNeutrons);

		if (AbsorbedNeutrons > NeutronSingularity)
		{
			Explosion.StartExplosion(CurrentlyInstalledIn.registerObject.WorldPositionServer, 200000);
			data.Break = true;
			if (this != null)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}


		return data;
	}
}
