using Objects;
using UnityEngine;

public class Canister : InputTrigger
{
	public GasContainer container;
	private Connector connector;
	private RegisterTile registerTile;

	private void Awake()
	{
		container = GetComponent<GasContainer>();
		registerTile = GetComponent<RegisterTile>();
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if(!CanUse(originator, hand, position, false)){
			return false;
		}
		if(!isServer){
			//ask server to perform the interaction
			InteractMessage.Send(gameObject, position, hand);
			return true;
		}

		PlayerNetworkActions pna = originator.GetComponent<PlayerNetworkActions>();
		GameObject handObj = pna.Inventory[hand].Item;
		if (handObj && handObj.GetComponent<WrenchTrigger>())
		{
			if(connector == null)
			{
				var foundConnectors = MatrixManager.GetAt<Connector>(registerTile.PositionServer, true);
				for (int n = 0; n < foundConnectors.Count; n++)
				{
					var conn = foundConnectors[n];
					if (conn.anchored)
					{
						connector = conn;
						connector.ConnectCanister(this);
						return true;
					}
				}
			}
			else
			{
				connector.DisconnectCanister();
				connector = null;
				return true;
			}
		}

		container.Opened = !container.Opened;

		string msg = container.Opened ? $"The valve is open, outputting at {container.ReleasePressure} kPa." : "The valve is closed.";
		UpdateChatMessage.Send(originator, ChatChannel.Examine, msg);

		return true;
	}
}