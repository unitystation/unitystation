using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class Connector : AdvancedPipe
{
	public Canister canister;

	public void ConnectCanister(Canister newCanister)
	{
		canister = newCanister;
	}

	public void DisconnectCanister()
	{
		canister = null;
	}

	public override void TickUpdate()
	{
		base.TickUpdate();
		if ( anchored && canister != null)
		{
			MergeAir();
		}
	}

	void MergeAir()
	{
		pipenet.gasMix.MergeGasMix(canister.container.GasMix);
	}

}
