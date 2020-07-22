using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class QuantumPad : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	public QuantumPad connectedPad;

	/// <summary>
	/// Detects players/objects on itself every 1 second.
	/// </summary>
	public bool passiveDetect;

	/// <summary>
	/// Where should this pad drop you on the next pad?
	/// </summary>
	public PadDirection padDirection = PadDirection.OnTop;

	/// <summary>
	/// If you dont want the link to be changed.
	/// </summary>
	public bool disallowLinkChange;

	public string messageOnTravelToThis;

	private RegisterTile registerTile;

	private Matrix Matrix => registerTile.Matrix;

	private Vector3 travelCoord;

	private SpriteHandler spriteHandler;

	private bool doingAnimation;

	/// <summary>
	/// Temp until shuttle landings possible
	/// </summary>
	public bool IsLavaLandBase1;

	/// <summary>
	/// Temp until shuttle landings possible
	/// </summary>
	public bool IsLavaLandBase1Connector;

	/// <summary>
	/// Temp until shuttle landings possible
	/// </summary>
	public bool IsLavaLandBase2;

	/// <summary>
	/// Temp until shuttle landings possible
	/// </summary>
	public bool IsLavaLandBase2Connector;

	[Server]
	private void ServerSync(bool newVar)
	{
		doingAnimation = newVar;
	}

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		spriteHandler.ChangeSprite(0);
	}

	private void Start()
	{
		//temp stuff

		if (IsLavaLandBase1)
		{
			LavaLandManager.Instance.LavaLandBase1 = this;
		}

		if (IsLavaLandBase2)
		{
			LavaLandManager.Instance.LavaLandBase2 = this;
		}

		if (IsLavaLandBase1Connector)
		{
			LavaLandManager.Instance.LavaLandBase1Connector = this;
		}

		if (IsLavaLandBase2Connector)
		{
			LavaLandManager.Instance.LavaLandBase2Connector = this;
		}
	}

	private void OnEnable()
	{
		if (!passiveDetect) return;
		UpdateManager.Add(DetectObjectsOnTile, 1f);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, DetectObjectsOnTile);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (Validations.IsTarget(gameObject, interaction)) return true;

		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		DetectObjectsOnTile();
	}

	public void DetectObjectsOnTile()
	{
		if(!CustomNetworkManager.IsServer) return;

		if(connectedPad == null) return;

		if (!doingAnimation && !passiveDetect)
		{
			ServerSync(true);

			StartCoroutine(ServerAnimation());
		}

		travelCoord = connectedPad.registerTile.WorldPositionServer;

		switch (padDirection)
		{
			case PadDirection.OnTop:
				break;
			case PadDirection.Up:
				travelCoord += Vector3.up;
				break;
			case PadDirection.Down:
				travelCoord += Vector3.down;
				break;
			case PadDirection.Left:
				travelCoord += Vector3.left;
				break;
			case PadDirection.Right:
				travelCoord += Vector3.right;
				break;
		}

		if (passiveDetect && padDirection == PadDirection.OnTop)
		{
			travelCoord += Vector3.up;
		}

		var message = connectedPad.messageOnTravelToThis;

		var registerTileLocation = registerTile.LocalPositionServer;

		//detect players positioned on the portal bit of the gateway
		var playersFound = Matrix.Get<ObjectBehaviour>(registerTileLocation, ObjectType.Player, true);

		var somethingTeleported = false;

		foreach (ObjectBehaviour player in playersFound)
		{
			Chat.AddLocalMsgToChat(message, travelCoord, gameObject);
			SoundManager.PlayNetworkedForPlayer(player.gameObject, "StealthOff"); //very weird, sometimes does the sound other times not.
			TransportPlayers(player);
			somethingTeleported = true;
		}

		foreach (var objects in Matrix.Get<ObjectBehaviour>(registerTileLocation, ObjectType.Object, true))
		{
			TransportObjectsItems(objects);
			somethingTeleported = true;
		}

		foreach (var items in Matrix.Get<ObjectBehaviour>(registerTileLocation, ObjectType.Item, true))
		{
			TransportObjectsItems(items);
			somethingTeleported = true;
		}

		if (!doingAnimation && passiveDetect && somethingTeleported)
		{
			ServerSync(true);

			StartCoroutine(ServerAnimation());
		}
	}

	public IEnumerator ServerAnimation()
	{
		spriteHandler.ChangeSprite(1);
		yield return WaitFor.Seconds(1f);
		spriteHandler.ChangeSprite(0);
		ServerSync(false);
	}


	[Server]
	public void TransportPlayers(ObjectBehaviour player)
	{
		//teleports player to the front of the new gateway
		player.GetComponent<PlayerSync>().SetPosition(travelCoord, true);
	}

	[Server]
	public void TransportObjectsItems(ObjectBehaviour objectsItems)
	{
		objectsItems.GetComponent<CustomNetTransform>().SetPosition(travelCoord);
	}

	public enum PadDirection
	{
		OnTop,
		Up,
		Down,
		Left,
		Right
	}
}
