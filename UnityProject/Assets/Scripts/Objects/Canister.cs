using Objects;
using UnityEngine;

public class Canister : InputTrigger
{
	public ObjectBehaviour objectBehaviour;
	public GasContainer container;
	private Connector connector;
	private RegisterTile registerTile;
	public SpriteRenderer connectorRenderer;
	public Sprite connectorSprite;

	private void Awake()
	{
		container = GetComponent<GasContainer>();
		registerTile = GetComponent<RegisterTile>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
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
				var foundConnectors = MatrixManager.GetAt<Connector>(registerTile.WorldPositionServer, true);
				for (int n = 0; n < foundConnectors.Count; n++)
				{
					var conn = foundConnectors[n];
					if (conn.objectBehaviour.isNotPushable)
					{
						connector = conn;
						connector.ConnectCanister(this);
						connectorRenderer.sprite = connectorSprite;
						objectBehaviour.isNotPushable = true;
						return true;
					}
				}
			}
			else
			{
				connector.DisconnectCanister();
				connectorRenderer.sprite = null;
				connector = null;
				objectBehaviour.isNotPushable = false;
				return true;
			}
		}

		container.Opened = !container.Opened;

		string msg = container.Opened ? $"The valve is open, outputting at {container.ReleasePressure} kPa." : "The valve is closed.";
		UpdateChatMessage.Send(originator, ChatChannel.Examine, msg);

		return true;
	}
}