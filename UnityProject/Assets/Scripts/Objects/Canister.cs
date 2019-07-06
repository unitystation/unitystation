using System;
using Objects;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Main component for canister
/// </summary>
[RequireComponent(typeof(Integrity))]
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
		SetDefaultIntegrity();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		//ensure we disconnect
		Disconnect();
	}

	private void Disconnect()
	{
		if (isConnected)
		{
			connector.DisconnectCanister();
			isConnected = false;
			connectorRenderer.sprite = null;
			SetConnectedSprite(null);
			objectBehaviour.isNotPushable = false;
		}
	}

	private void SetDefaultIntegrity()
	{
		//default canister integrity values
		GetComponent<Integrity>().HeatResistance = 1000;
		GetComponent<Integrity>().Armor = new Armor
		{
			Melee = 50,
			Bullet = 50,
			Laser = 50,
			Energy = 100,
			Bomb = 10,
			Bio = 100,
			Rad = 100,
			Fire = 80,
			Acid = 50
		};
	}

	//this is just here so anyone trying to change the armor value in inspector sees it being
	//reset
	private void OnValidate()
	{
		SetDefaultIntegrity();
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
				Disconnect();
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