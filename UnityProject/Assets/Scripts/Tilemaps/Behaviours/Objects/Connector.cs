using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class Connector : AdvancedPipe
{
	private Canister canister;

	private void Start() {
		UpdateManager.Instance.Add(UpdateMe);
	}

	public void ConnectCanister(Canister newCanister)
	{
		canister = newCanister;
	}

	public void DisconnectCanister()
	{
		canister = null;
	}

	void UpdateMe()
	{
		if (objectBehaviour.isNotPushable && canister != null)
		{
			MergeAir();
		}
	}

	void MergeAir()
	{
		pipenet.gasMix.MergeGasMix(canister.container.GasMix);
	}

}
