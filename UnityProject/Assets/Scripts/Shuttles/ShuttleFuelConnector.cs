using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.GasContainer;

namespace Pipes
{
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
