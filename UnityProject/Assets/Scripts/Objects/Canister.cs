using Objects;
using UnityEngine;
using UnityEngine.Networking;

public class Canister : NBHandApplyInteractable
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


	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return InteractionValidationChain<HandApply>.Create()
			.WithValidation(TargetIs.GameObject(gameObject))
			.WithValidation(CanApply.ONLY_IF_CONSCIOUS);
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{

		PlayerNetworkActions pna = interaction.Performer.GetComponent<PlayerNetworkActions>();
		GameObject handObj = pna.Inventory[interaction.HandSlot.SlotName].Item;
		var tool = handObj != null ? handObj.GetComponent<Tool>() : null;
		//can click on the canister with a wrench to connect/disconnect it from a connector
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
				return;
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
						return;
					}
				}
			}
		}

		//can open/close connector by clicking on it without a wrench
		container.Opened = !container.Opened;

		string msg = container.Opened ? $"The valve is open, outputting at {container.ReleasePressure} kPa." : "The valve is closed.";
		UpdateChatMessage.Send(interaction.Performer, ChatChannel.Examine, msg);
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