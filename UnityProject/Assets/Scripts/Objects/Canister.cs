using System;
using Objects;
using UnityEngine;
using Mirror;

/// <summary>
/// Main component for canister
/// </summary>
[RequireComponent(typeof(Integrity))]
public class Canister : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	public static readonly int MAX_RELEASE_PRESSURE = 1000;
	[Tooltip("Tint of the main background in the GUI")]
	public Color UIBGTint;
	[Tooltip("Tint of the inner panel in the GUI")]
	public Color UIInnerPanelTint;
	[Tooltip("Name to show for the contents of this canister in the GUI")]
	public String ContentsName;
	public ObjectBehaviour objectBehaviour;
	public GasContainer container;
	public bool hasContainerInserted;
	public GameObject InsertedContainer {get; set;}
	public Connector connector;
	[SyncVar(hook = nameof(SyncConnected))]
	public bool isConnected;
	private RegisterTile registerTile;
	public SpriteRenderer connectorRenderer;
	public ShuttleFuelConnector connectorFuel;
	public Sprite connectorSprite;
	/// <summary>
	/// Invoked on server side when connection status changes, provides a bool indicating
	/// if it is connected.
	///
	/// NOTE: Doesn't need to be server side since isConnected is a sync var (and thus
	/// is available to the client), but I'm not sure
	/// it's always going to be a syncvar so I'm making this hook server only.
	/// </summary>
	/// <returns></returns>
	[NonSerialized]
	public BoolEvent ServerOnConnectionStatusChange = new BoolEvent();
	[NonSerialized]
	public BoolEvent ServerOnExternalTankInserted = new BoolEvent();

	private void Awake()
	{
		EnsureInit();
		SetDefaultIntegrity();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void EnsureInit()
	{
		if (container != null) return;
		container = GetComponent<GasContainer>();
		registerTile = GetComponent<RegisterTile>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
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
			if (connector != null)
			{
				connector.DisconnectCanister();
			}
			else if (connectorFuel != null){

				connectorFuel.DisconnectCanister();
			}
			isConnected = false;
			connectorRenderer.sprite = null;
			SetConnectedSprite(null);
			objectBehaviour.ServerSetPushable(true);
			ServerOnConnectionStatusChange.Invoke(false);
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

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//using wrench
		if (DefaultWillInteract.HandApply(interaction, side) &&
			Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
			return true;
		//using any fillable gas container
		else if (DefaultWillInteract.HandApply(interaction, side) &&
				 Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.CanisterFillable))
			return true;
		else
			return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{

		PlayerNetworkActions pna = interaction.Performer.GetComponent<PlayerNetworkActions>();
		var handObj = interaction.HandObject;
		var playerPerformer = interaction.Performer;
		ConnectedPlayer currentPlayer = playerPerformer.Player();

		//can click on the canister with a wrench to connect/disconnect it from a connector
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			if (isConnected)
			{
				SoundManager.PlayNetworkedAtPos("Wrench", registerTile.WorldPositionServer, 1f);
				Disconnect();
				return;
			}
			else
			{
				var foundConnectors = registerTile.Matrix.Get<Connector>(registerTile.LocalPositionServer, true);
				foreach (var conn in foundConnectors)
				{
					if (conn.ObjectBehavior.IsNotPushable)
					{
						SoundManager.PlayNetworkedAtPos("Wrench", registerTile.WorldPositionServer, 1f);
						connector = conn;
						isConnected = true;
						connector.ConnectCanister(this);
						SetConnectedSprite(connectorSprite);
						objectBehaviour.ServerSetPushable(false);
						ServerOnConnectionStatusChange.Invoke(true);
						return;
					}
				}

				var foundFuelConnectors = registerTile.Matrix.Get<ShuttleFuelConnector>(registerTile.LocalPositionServer, true);
				foreach (var conn in foundFuelConnectors)
				{
					SoundManager.PlayNetworkedAtPos("Wrench", registerTile.WorldPositionServer, 1f);
					isConnected = true;
					connectorFuel = conn;
					conn.ConnectCanister(this);
					SetConnectedSprite(connectorSprite);
					objectBehaviour.ServerSetPushable(false);
					ServerOnConnectionStatusChange.Invoke(true);
				}
			}
		}

		//can click on the canister with a refillable tank to insert the refillable tank into the canister
		if (Validations.HasItemTrait(handObj, CommonTraits.Instance.CanisterFillable))
		{
			//don't insert a container if one is already present, lest we wipe out the previous container from existance
			if (!hasContainerInserted)
			{
				//always null check... always...
				if (handObj.GetComponent<GasContainer>() != null)
				{
					//copy the containers properties over, delete the container from the player's hand
					InsertedContainer = handObj;
					hasContainerInserted = true;
					Chat.AddActionMsgToChat(playerPerformer, $"You insert the {handObj.ExpensiveName()} into the canister.",
											$"{playerPerformer.ExpensiveName()} inserts a tank into the {this.ContentsName} tank.");
					Inventory.ServerDespawn(handObj);
					ServerOnExternalTankInserted.Invoke(true);
				}
				else
				{
					Logger.LogError("Player tried inserting a tank into a canister, but the tank didn't have a GasContainer "+
									"component associated with it. Something terrible has happened, or an item that should not "+
									"has the CanisterFillable ItemTrait.");
				}
			}
			else
			{
				Chat.AddExamineMsg(playerPerformer, "A tank is already inside this canister.");
			}
		}
	}

	public override void OnStartClient()
	{
		EnsureInit();
		SyncConnected(isConnected, isConnected);
	}

	void SetConnectedSprite(Sprite value)
	{
		connectorRenderer.sprite = value;
	}

	void SyncConnected(bool oldValue, bool value)
	{
		EnsureInit();
		if (value)
		{
			SetConnectedSprite(connectorSprite);
		}
		else
		{
			SetConnectedSprite(null);
		}
	}

	public void EjectInsertedContainer()
	{
		//create a new container based on the one currently in the canister, copy the properties over to it
		Spawn.ServerClone(InsertedContainer, this.registerTile.WorldPositionServer);
		hasContainerInserted = false;
		InsertedContainer = null;
		ServerOnExternalTankInserted.Invoke(false);
	}
}