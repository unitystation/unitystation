using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
///     ** Now all movement input keys are sent to PlayerSync.Client
/// 	** PlayerMove may become obsolete in the future
///     It also changes the sprite direction and
///     handles interaction with objects that can
///     be walked into it.
/// </summary>
public class PlayerMove : NetworkBehaviour, IRightClickable, IServerSpawn, IActionGUI, ICheckedInteractable<ContextMenuApply>
{
	public PlayerScript PlayerScript => playerScript;

	public bool diagonalMovement;

	[SyncVar] public bool allowInput = true;

	//netid of the game object we are buckled to, NetId.Empty if not buckled
	[SyncVar(hook = nameof(SyncBuckledObjectNetId))]
	private uint buckledObjectNetId = NetId.Empty;

	/// <summary>
	/// Object this player is buckled to (if buckled). Null if not buckled.
	/// </summary>
	public GameObject BuckledObject => buckledObject;
	//cached for fast access
	private GameObject buckledObject;

	//callback invoked when we are unbuckled.
	private Action onUnbuckled;

	/// <summary>
	/// Whether character is buckled to a chair
	/// </summary>
	public bool IsBuckled => BuckledObject != null;

	[SyncVar(hook = nameof(SyncCuffed))] private bool cuffed;

	/// <summary>
	/// Whether the character is restrained with handcuffs (or similar)
	/// </summary>
	public bool IsCuffed => cuffed;

	/// <summary>
	/// Invoked on server side when the cuffed state is changed
	/// </summary>
	[NonSerialized]
	public CuffEvent OnCuffChangeServer = new CuffEvent();

	/// <summary>
	/// Whether this player meets all the conditions for being swapped with, but only
	/// for the conditions the client is not allowed to know
	/// (help intent, not pulling anything - clients aren't informed of these things for each player).
	/// Doesn't incorporate any other conditions into this
	/// flag, but IsSwappable does.
	/// </summary>
	[SyncVar]
	private bool isSwappable;

	/// <summary>
	/// server side only, tracks whether this player has indicated they are on help intent. Used
	/// for checking for swaps.
	/// </summary>
	public bool IsHelpIntentServer => isHelpIntentServer;
	//starts true because all players spawn with help intent.
	private bool isHelpIntentServer = true;


	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	/// <summary>
	/// Whether this player meets all the conditions for being swapped with (being the swapee).
	/// </summary>
	public bool IsSwappable
	{
		get
		{
			bool canSwap;
			if (isLocalPlayer && !isServer)
			{
				if (playerScript.pushPull == null)
				{
					//Is a ghost
					canSwap = false;
				}
				else
				{
					//locally predict
					canSwap = UIManager.CurrentIntent == Intent.Help
					          && !PlayerScript.pushPull.IsPullingSomething;
				}
			}
			else
			{
				//rely on server synced value
				canSwap = isSwappable;
			}
			return canSwap
			       //don't swap with ghosts
			       && !PlayerScript.IsGhost
			       //pass through players if we can
			       && !registerPlayer.IsPassable(isServer)
			       //can't swap with buckled players, they're strapped down
			       && !IsBuckled;
		}
	}

	private readonly List<MoveAction> moveActionList = new List<MoveAction>();

	public MoveAction[] moveList =
	{
		MoveAction.MoveUp, MoveAction.MoveLeft, MoveAction.MoveDown, MoveAction.MoveRight
	};

	private Directional playerDirectional;

	[HideInInspector] public PlayerNetworkActions pna;

	[HideInInspector] [SyncVar(hook = nameof(SyncRunSpeed))] public float RunSpeed;
	[HideInInspector] [SyncVar(hook = nameof(SyncWalkSpeed))] public float WalkSpeed;
	[HideInInspector] public float CrawlSpeed;

	/// <summary>
	/// Player will fall when pushed with such speed
	/// </summary>
	public float PushFallSpeed = 10;

	private RegisterPlayer registerPlayer;
	private Matrix matrix => registerPlayer.Matrix;
	private PlayerScript playerScript;

	private void Awake()
	{
		playerScript = GetComponent<PlayerScript>();
	}

	private void Start()
	{
		playerDirectional = gameObject.GetComponent<Directional>();

		registerPlayer = GetComponent<RegisterPlayer>();
		pna = gameObject.GetComponent<PlayerNetworkActions>();
		RunSpeed = 6;
		WalkSpeed = 3;
		CrawlSpeed = 0.8f;
	}

	public override void OnStartClient()
	{
		SyncCuffed(cuffed, this.cuffed);
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		//when pulling status changes, re-check whether client needs to be told if
		//this is swappable.
		if (playerScript.pushPull != null)
		{
			playerScript.pushPull.OnPullingSomethingChangedServer.AddListener(ServerUpdateIsSwappable);
		}

		ServerUpdateIsSwappable();
	}

	public Vector3Int GetNextPosition(Vector3Int currentPosition, PlayerAction action, bool isReplay,
		Matrix curMatrix = null)
	{
		if (!curMatrix)
		{
			curMatrix = matrix;
		}

		Vector3Int direction = GetDirection(action, MatrixManager.Get(curMatrix), isReplay);

		return currentPosition + direction;
	}

	private Vector3Int GetDirection(PlayerAction action, MatrixInfo matrixInfo, bool isReplay)
	{
		ProcessAction(action);

		if (diagonalMovement)
		{
			return GetMoveDirection(matrixInfo, isReplay);
		}

		if (moveActionList.Count > 0)
		{
			return GetMoveDirection(moveActionList[moveActionList.Count - 1]);
		}

		return Vector3Int.zero;
	}

	private void ProcessAction(PlayerAction action)
	{
		List<int> actionKeys = new List<int>(action.moveActions);

		for (int i = 0; i < moveList.Length; i++)
		{
			if (actionKeys.Contains((int)moveList[i]) && !moveActionList.Contains(moveList[i]))
			{
				moveActionList.Add(moveList[i]);
			}
			else if (!actionKeys.Contains((int)moveList[i]) && moveActionList.Contains(moveList[i]))
			{
				moveActionList.Remove(moveList[i]);
			}
		}
	}

	private Vector3Int GetMoveDirection(MatrixInfo matrixInfo, bool isReplay)
	{
		Vector3Int direction = Vector3Int.zero;

		for (int i = 0; i < moveActionList.Count; i++)
		{
			direction += GetMoveDirection(moveActionList[i]);
		}

		direction.x = Mathf.Clamp(direction.x, -1, 1);
		direction.y = Mathf.Clamp(direction.y, -1, 1);
		//			Logger.LogTrace(direction.ToString(), Category.Movement);

		if (matrixInfo.MatrixMove)
		{
			// Converting world direction to local direction
			direction = Vector3Int.RoundToInt(matrixInfo.MatrixMove.FacingOffsetFromInitial.QuaternionInverted *
			                                  direction);
		}


		return direction;
	}

	private Vector3Int GetMoveDirection(MoveAction action)
	{
		if (PlayerManager.LocalPlayer == gameObject && UIManager.IsInputFocus)
		{
			return Vector3Int.zero;
		}

		switch (action)
		{
			case MoveAction.MoveUp:
				return Vector3Int.up;
			case MoveAction.MoveLeft:
				return Vector3Int.left;
			case MoveAction.MoveDown:
				return Vector3Int.down;
			case MoveAction.MoveRight:
				return Vector3Int.right;
		}

		return Vector3Int.zero;
	}

	/// <summary>
	/// Buckle the player at their current position.
	/// </summary>
	/// <param name="toObject">object to which they should be buckled, must have network instance id.</param>
	/// <param name="unbuckledAction">callback to invoke when we become unbuckled</param>
	[Server]
	public void ServerBuckle(GameObject toObject, Action unbuckledAction = null)
	{
		var netid = toObject.NetId();
		if (netid == NetId.Invalid)
		{
			Logger.LogError("attempted to buckle to object " + toObject + " which has no NetworkIdentity. Buckle" +
			                " can only be used on objects with a Net ID. Ensure this object has one.",
				Category.Movement);
			return;
		}

		var buckleInteract = toObject.GetComponent<BuckleInteract>();

		if (buckleInteract.forceLayingDown)
		{
			registerPlayer.ServerLayDown();
		}
		else
		{
			registerPlayer.ServerStandUp();
		}

		SyncBuckledObjectNetId(0, netid);
		//can't push/pull when buckled in, break if we are pulled / pulling
		//inform the puller
		if (PlayerScript.pushPull.PulledBy != null)
		{
			PlayerScript.pushPull.PulledBy.ServerStopPulling();
		}

		PlayerScript.pushPull.StopFollowing();
		PlayerScript.pushPull.ServerStopPulling();
		PlayerScript.pushPull.ServerSetPushable(false);
		onUnbuckled = unbuckledAction;

		//sync position to ensure they buckle to the correct spot
		playerScript.PlayerSync.SetPosition(toObject.TileWorldPosition().To3Int());

		//set direction if toObject has a direction
		var directionalObject = toObject.GetComponent<Directional>();
		if (directionalObject != null)
		{
			playerDirectional.FaceDirection(directionalObject.CurrentDirection);
		}
		else
		{
			playerDirectional.FaceDirection(playerDirectional.CurrentDirection);
		}

		//force sync direction to current direction (If it is a real player and not a NPC)
		if (PlayerScript.connectionToClient != null)
			playerDirectional.TargetForceSyncDirection(PlayerScript.connectionToClient);
	}

	/// <summary>
	/// Unbuckle the player when they are currently buckled..
	/// </summary>
	[Command]
	public void CmdUnbuckle()
	{
		Unbuckle();
	}

	/// <summary>
	/// Server side logic for unbuckling a player
	/// </summary>
	[Server]
	public void Unbuckle()
	{
		var previouslyBuckledTo = BuckledObject;
		SyncBuckledObjectNetId(0, NetId.Empty);
		//we can be pushed / pulled again
		PlayerScript.pushPull.ServerSetPushable(true);
		//decide if we should fall back down when unbuckled
		registerPlayer.ServerSetIsStanding(PlayerScript.playerHealth.ConsciousState == ConsciousState.CONSCIOUS);
		onUnbuckled?.Invoke();

		if (previouslyBuckledTo == null) return;

		var integrityBuckledObject = previouslyBuckledTo.GetComponent<Integrity>();
		if(integrityBuckledObject != null) integrityBuckledObject.OnServerDespawnEvent -= Unbuckle;

		//we are unbuckled but still will drift with the object.
		var buckledCNT = previouslyBuckledTo.GetComponent<CustomNetTransform>();
		if (buckledCNT.IsFloatingServer)
		{
			playerScript.PlayerSync.NewtonianMove(buckledCNT.ServerImpulse.NormalizeToInt(), buckledCNT.SpeedServer);
		}
		else
		{
			//stop in place because our object wasn't moving either.
			playerScript.PlayerSync.Stop();
		}
	}

	//invoked when buckledTo changes direction, so we can update our direction
	private void OnBuckledObjectDirectionChange(Orientation newDir)
	{
		if (playerDirectional == null)
		{
			playerDirectional = gameObject.GetComponent<Directional>();
		}
		playerDirectional.FaceDirection(newDir);
	}

	//syncvar hook invoked client side when the buckledTo changes
	private void SyncBuckledObjectNetId(uint oldBuckledTo, uint newBuckledTo)
	{
		//unsub if we are subbed
		if (IsBuckled)
		{
			var directionalObject = BuckledObject.GetComponent<Directional>();
			if (directionalObject != null)
			{
				directionalObject.OnDirectionChange.RemoveListener(OnBuckledObjectDirectionChange);
			}
		}

		if (PlayerManager.LocalPlayer == gameObject)
		{
			UIActionManager.ToggleLocal(this, newBuckledTo != NetId.Empty);
		}

		buckledObjectNetId = newBuckledTo;
		buckledObject = NetworkUtils.FindObjectOrNull(buckledObjectNetId);

		//sub
		if (buckledObject != null)
		{
			var directionalObject = buckledObject.GetComponent<Directional>();
			if (directionalObject != null)
			{
				directionalObject.OnDirectionChange.AddListener(OnBuckledObjectDirectionChange);
			}
		}

		//ensure we are in sync with server
		playerScript?.PlayerSync?.RollbackPrediction();
	}

	/// <summary>
	/// Changes the player speed from Server. Values inputted as arguments will OVERRIDE the current speed!
	/// </summary>
	/// <param name="run">At what speed should the player run</param>
	/// <param name="walk">At what speed should the player walk</param>
	[Server]
	public void ServerChangeSpeed(float run = 0f, float walk = 0f)
	{
		RunSpeed = run < CrawlSpeed ? CrawlSpeed : run;
		WalkSpeed = walk < CrawlSpeed ? CrawlSpeed : walk;
	}

	private void SyncRunSpeed(float oldSpeed, float newSpeed)
	{
		this.RunSpeed = newSpeed;
	}

	private void SyncWalkSpeed(float oldSpeed, float newSpeed)
	{
		this.WalkSpeed = newSpeed;
	}

	public void CallActionClient()
	{
		if (CanUnBuckleSelf())
		{
			CmdUnbuckle();
		}
	}

	private bool CanUnBuckleSelf()
	{
		PlayerHealth playerHealth = playerScript.playerHealth;

		return !(playerHealth == null ||
		         playerHealth.ConsciousState == ConsciousState.DEAD ||
		         playerHealth.ConsciousState == ConsciousState.UNCONSCIOUS ||
		         playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS);
	}

	/// <summary>
	/// Tell the server we are now on or not on help intent. This only affects
	/// whether we are swappable or not. Other than this the client never tells the
	/// server their current intent except when sending an interaction message.
	/// A hacked client could lie about this but not a huge issue IMO.
	/// </summary>
	/// <param name="helpIntent">are we now on help intent</param>
	[Command]
	public void CmdSetHelpIntent(bool helpIntent)
	{
		isHelpIntentServer = helpIntent;
		ServerUpdateIsSwappable();
	}

	/// <summary>
	/// Checks if the conditions for swappability that aren't
	/// known by clients are met and updates the syncvar.
	/// </summary>
	private void ServerUpdateIsSwappable()
	{
		isSwappable = isHelpIntentServer && PlayerScript != null &&
		              PlayerScript.pushPull != null &&
		              !PlayerScript.pushPull.IsPullingSomethingServer;
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncCuffed(cuffed, this.cuffed);
	}

	#region Cuffing

	/// <summary>
	/// Anything with PlayerMove can be cuffed and uncuffed. Might make sense to seperate that into its own behaviour
	/// </summary>
	/// <returns>The menu including the uncuff action if applicable, otherwise null</returns>
	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (!WillInteract(ContextMenuApply.ByLocalPlayer(gameObject, "Uncuff"), NetworkSide.Client)) return result;

		return result.AddElement("Uncuff", OnUncuffClicked);
	}

	/// <summary>
	/// Used for the right click action, sends a message requesting uncuffing
	/// </summary>
	public void OnUncuffClicked()
	{
		RequestInteractMessage.Send(ContextMenuApply.ByLocalPlayer(gameObject, "Uncuff"), this);
	}

	/// <summary>
	/// Determines if the interaction request for uncuffing is valid clientside and if true, then serverside
	/// </summary>
	public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return cuffed;
	}

	/// <summary>
	/// Handles the interaction request for uncuffing serverside
	/// </summary>
	public void ServerPerformInteraction(ContextMenuApply interaction)
	{
		var handcuffs = interaction.TargetObject.GetComponent<ItemStorage>().GetNamedItemSlot(NamedSlot.handcuffs).ItemObject;
		if (handcuffs == null) return;

		var restraint = handcuffs.GetComponent<Restraint>();
		if (restraint == null) return;

		var ProgressConfig = new StandardProgressActionConfig(StandardProgressActionType.Uncuff);
		StandardProgressAction.Create(ProgressConfig, Uncuff)
			.ServerStartProgress(interaction.TargetObject.RegisterTile(), restraint.RemoveTime, interaction.Performer);
	}

	[Server]
	public void Cuff(HandApply interaction)
	{
		SyncCuffed(cuffed, true);

		var targetStorage = interaction.TargetObject.GetComponent<ItemStorage>();

		//transfer cuffs to the special cuff slot
		ItemSlot handcuffSlot = targetStorage.GetNamedItemSlot(NamedSlot.handcuffs);
		Inventory.ServerTransfer(interaction.HandSlot, handcuffSlot);

		//drop hand items
		Inventory.ServerDrop(targetStorage.GetNamedItemSlot(NamedSlot.leftHand));
		Inventory.ServerDrop(targetStorage.GetNamedItemSlot(NamedSlot.rightHand));

		if (connectionToClient != null) TargetPlayerUIHandCuffToggle(connectionToClient, true);
	}

	[TargetRpc]
	private void TargetPlayerUIHandCuffToggle(NetworkConnection target, bool activeState)
	{
		Sprite leftSprite = null;
		Sprite rightSprite = null;

		if (activeState)
		{
			leftSprite = UIManager.Hands.LeftHand.GetComponentInParent<Handcuff>().HandcuffSprite;
			rightSprite = UIManager.Hands.RightHand.GetComponentInParent<Handcuff>().HandcuffSprite;
		}

		UIManager.Hands.LeftHand.SetSecondaryImage(leftSprite);
		UIManager.Hands.RightHand.SetSecondaryImage(rightSprite);
	}

	/// <summary>
	/// Request a ContextMenuApply interaction if you have not done your own validation.
	/// Calling this clientside will break your client.
	/// </summary>
	[Server]
	public void Uncuff()
	{
		SyncCuffed(cuffed, false);

		Inventory.ServerDrop(playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.handcuffs));
		TargetPlayerUIHandCuffToggle(connectionToClient, false);
	}

	private void SyncCuffed(bool wasCuffed, bool cuffed)
	{
		var oldCuffed = this.cuffed;
		this.cuffed = cuffed;

		if (isServer)
		{
			OnCuffChangeServer.Invoke(oldCuffed, this.cuffed);
		}
	}

	#endregion Cuffing
}

/// <summary>
/// Cuff state changed, provides old state and new state as 1st and 2nd args
/// </summary>
public class CuffEvent : UnityEvent<bool, bool>
{
}
