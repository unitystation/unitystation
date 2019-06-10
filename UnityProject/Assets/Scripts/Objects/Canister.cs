using Objects;
using UnityEngine;
using UnityEngine.Networking;

public class Canister : InputTrigger
{
	public ObjectBehaviour objectBehaviour;
	public GasContainer container;
	public Connector connector;
	[SyncVar(hook = nameof(SyncConnected))] public bool isConnected;
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
		var tool = handObj != null ? handObj.GetComponent<Tool>() : null;
		if (tool != null && tool.ToolType == ToolType.Wrench)
		{
			if(isConnected)
			{
				SoundManager.PlayNetworkedAtPos("Wrench", registerTile.WorldPositionServer, 1f);
				connector.DisconnectCanister();
				isConnected = false;
				connectorRenderer.sprite = null;
				SetConnectedSprite(null);
				objectBehaviour.isNotPushable = false;
				return true;
			}
			else
			{
				var foundConnectors = MatrixManager.GetAt<Connector>(registerTile.WorldPositionServer, true);
				for (int n = 0; n < foundConnectors.Count; n++)
				{
					var conn = foundConnectors[n];
					if (conn.objectBehaviour.isNotPushable)
					{
						SoundManager.PlayNetworkedAtPos("Wrench", registerTile.WorldPositionServer, 1f);
						connector = conn;
						isConnected = true;
						connector.ConnectCanister(this);
						SetConnectedSprite(connectorSprite);
						objectBehaviour.isNotPushable = true;
						return true;
					}
				}
			}
		}

		container.Opened = !container.Opened;

		string msg = container.Opened ? $"The valve is open, outputting at {container.ReleasePressure} kPa." : "The valve is closed.";
		UpdateChatMessage.Send(originator, ChatChannel.Examine, msg);

		return true;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SyncConnected(isConnected);
	}

	void SetConnectedSprite(Sprite value)
	{
		connectorRenderer.sprite = value;
	}

	void SyncConnected(bool value)
	{
		if(value)
		{
			SetConnectedSprite(connectorSprite);
		}
		else
		{
			SetConnectedSprite(null);
		}
	}
}