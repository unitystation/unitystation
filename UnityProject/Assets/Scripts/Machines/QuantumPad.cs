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

		var somethingTeleported = false;

		//Use the transport object code from StationGateway

		//detect players positioned on the portal bit of the gateway
		foreach (ObjectBehaviour player in Matrix.Get<ObjectBehaviour>(registerTileLocation, ObjectType.Player, true))
		{
			Chat.AddLocalMsgToChat(message, travelCoord, gameObject);
			SoundManager.PlayNetworkedForPlayer(player.gameObject, "StealthOff"); //very weird, sometimes does the sound other times not.
			TransportUtility.TransportObjectAndPulled(player, travelCoord);
			somethingTeleported = true;
		}

		//detect objects and items
		foreach (var item in Matrix.Get<ObjectBehaviour>(registerTileLocation, ObjectType.Object, true)
								.Concat(Matrix.Get<ObjectBehaviour>(registerTileLocation, ObjectType.Item, true)))
		{
			TransportUtility.TransportObjectAndPulled(item, travelCoord);
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
	private void TransportObject(PushPull pushPullObject)
	{
		if (pushPullObject == null)
			return; //Don't even bother...

		//Handle PlayerSync and CustomNetTransform (No shared base SetPosition call)
		//Use Matrix.Get because for some reason that works better than directly getting the PlayerSync or CustomNetTransform components of the pulled object
		//Have the object disappear, set position, and appear at the target position,rollback prediction to avoid all lerps,
		//No common base class, so unforunately duplicated code is unavoidable.

		//Player objects get PlayerSync
		var player = Matrix.Get<ObjectBehaviour>(pushPullObject.registerTile.LocalPositionServer, ObjectType.Player, true)
			.FirstOrDefault(pulled => pulled == pushPullObject)
			?.GetComponent<PlayerSync>();
		if (player != null)
		{
			player.DisappearFromWorldServer();
			player.AppearAtPositionServer(travelCoord);
			player.RollbackPrediction();
		}
		//Object and Item objects get CustomNetTransform
		var obj = Matrix.Get<ObjectBehaviour>(pushPullObject.registerTile.LocalPositionServer, ObjectType.Object, true)
			.Concat(Matrix.Get<ObjectBehaviour>(pushPullObject.registerTile.LocalPositionServer, ObjectType.Item, true))
			.FirstOrDefault(pulled => pulled == pushPullObject)
			?.GetComponent<CustomNetTransform>();
		if (obj != null)
		{
			obj.DisappearFromWorldServer();
			obj.AppearAtPositionServer(travelCoord);
			obj.RollbackPrediction();
		}
	}

	[Server]
	private void TransportObjectAndPulled(PushPull pushPullObject)
	{
		var linkedList = new LinkedList<PushPull>();

		//Iterate the chain of linkage
		//The list will be used to rebuild the chain of pulling through the teleporter.
		//Ensure that no matter what, if some object in the chain is pulling the original object, the chain is broken there.

		//Start with the start object
		linkedList.AddFirst(pushPullObject);

		//Add all the things it pulls in a chain
		for(var currentObj = pushPullObject; currentObj.IsPullingSomething && currentObj.PulledObject != pushPullObject; currentObj = currentObj.PulledObject)
		{
			linkedList.AddLast(currentObj.PulledObject);
		}

		for (var node = linkedList.First; node != null; node = node.Next?.Next) //Current and next object are handled each cycle
		{
			var currentObj = node?.Value;
			var pulledObj = node?.Next?.Value;

			//Disconnect pulling to make it not be a problem
			currentObj.CmdStopPulling();

			//Transport current
			TransportObject(currentObj);
			if (pulledObj != null)
			{
				TransportObject(pulledObj);
				//Try to pull it again
				currentObj?.CmdPullObject(pulledObj?.gameObject);
			}
		}
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
