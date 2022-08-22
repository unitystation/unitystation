using System.Collections.Generic;
using UnityEngine;
using Objects.Atmospherics;


namespace Systems.Shuttles
{
	// TODO: deprecate this in favour of normal atmospheric Connector.
	public class ShuttleFuelConnector : MonoBehaviour
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
	}
}
