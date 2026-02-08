using UnityEngine;
using US13.Objects.Canisters;

namespace US13.Shuttles
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
