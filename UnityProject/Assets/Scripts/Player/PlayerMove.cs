using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
///     Player move queues the directional move keys
///     to be processed along with the server.
///     It also changes the sprite direction and
///     handles interaction with objects that can
///     be walked into it.
/// </summary>
public class PlayerMove : NetworkBehaviour, IRightClickable, IServerSpawn
{
	[SerializeField]
	private PlayerScript playerScript;
	public PlayerScript PlayerScript => playerScript;

	public bool diagonalMovement;

	[SyncVar] public bool allowInput = true;

	//netid of the game object we are buckled to, NetId.Invalid if not buckled
	[SyncVar(hook = nameof(OnBuckledChangedHook))]
	public uint buckledObject = NetId.Invalid;

	//callback invoked when we are unbuckled.
	private Action onUnbuckled;

	/// <summary>
	/// Whether character is buckled to a chair
	/// </summary>
	public bool IsBuckled => buckledObject != NetId.Invalid;

	[SyncVar(hook=nameof(SyncCuffed))] private bool cuffed;

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
	/// Tracks the server's idea of whether we have help intent
	/// </summary>
	[SyncVar] private bool serverIsHelpIntent = true;

	/// <summary>
	/// Tracks our idea of whether we have help intent so we can use it for client prediction
	/// </summary>
	private bool localIsHelpIntent = true;

	/// <summary>
	/// True iff this player is set to help intent, thus should swap places with players
	/// that they collide with if the other player also has help intent
	/// </summary>
	public bool IsHelpIntent
	{
		get
		{
			if (isLocalPlayer)
			{
				return localIsHelpIntent;
			}
			else
			{
				return serverIsHelpIntent;
			}
		}
		set
		{
			if (isLocalPlayer)
			{
				localIsHelpIntent = value;
				//tell the server we want this to be our setting
				CmdChangeHelpIntent(value);
			}
			else
			{
				//accept what the server is telling us about someone other than our local player
				serverIsHelpIntent = value;
			}
		}
	}

	private readonly List<MoveAction> moveActionList = new List<MoveAction>();

	public MoveAction[] moveList =
	{
		MoveAction.MoveUp, MoveAction.MoveLeft, MoveAction.MoveDown, MoveAction.MoveRight
	};

	private Directional playerDirectional;

	[HideInInspector] public PlayerNetworkActions pna;

	[FormerlySerializedAs("speed")] public float RunSpeed = 6;
	public float WalkSpeed = 3;
	public float CrawlSpeed = 0.8f;

	/// <summary>
	/// Player will fall when pushed with such speed
	/// </summary>
	public float PushFallSpeed = 10;

	private RegisterPlayer registerPlayer;
	private Matrix matrix => registerPlayer.Matrix;

	private void Start()
	{
		playerDirectional = gameObject.GetComponent<Directional>();

		registerPlayer = GetComponent<RegisterPlayer>();
		pna = gameObject.GetComponent<PlayerNetworkActions>();
	}

	public override void OnStartClient()
	{
		SyncCuffed(this.cuffed);
		base.OnStartClient();
	}

	[Command]
	private void CmdChangeHelpIntent(bool isHelpIntent)
	{
		serverIsHelpIntent = isHelpIntent;
	}

	public PlayerAction SendAction()
	{
		List<int> actionKeys = new List<int>();

		for (int i = 0; i < moveList.Length; i++)
		{
			if (PlayerManager.LocalPlayer == gameObject && UIManager.IsInputFocus)
			{
				return new PlayerAction {moveActions = actionKeys.ToArray()};
			}

			// if (CommonInput.GetKey(moveList[i]) && allowInput)
			// {
			// 	actionKeys.Add((int)moveList[i]);
			// }
			if (KeyboardInputManager.CheckMoveAction(moveList[i]))
			{
				if(allowInput && !IsBuckled && !IsCuffed){
					actionKeys.Add((int)moveList[i]);
				}
				else
				{
					if(PlayerScript.playerHealth.IsDead)
					{
						pna.CmdSpawnPlayerGhost();
					}
				}
			}
		}

		return new PlayerAction {moveActions = actionKeys.ToArray()};
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
			if (actionKeys.Contains((int) moveList[i]) && !moveActionList.Contains(moveList[i]))
			{
				moveActionList.Add(moveList[i]);
			}
			else if (!actionKeys.Contains((int) moveList[i]) && moveActionList.Contains(moveList[i]))
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
			direction = Vector3Int.RoundToInt(matrixInfo.MatrixMove.ClientState.FacingOffsetFromInitial(matrix.MatrixMove).QuaternionInverted *
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
		//no matter what, we stand up when buckled in
		registerPlayer.ServerStandUp();

		OnBuckledChangedHook(netid);
		//can't push/pull when buckled in, break if we are pulled / pulling
		//inform the puller
		if (PlayerScript.pushPull.PulledBy != null)
		{
			PlayerScript.pushPull.PulledBy.CmdStopPulling();
		}
		PlayerScript.pushPull.CmdStopFollowing();
		PlayerScript.pushPull.CmdStopPulling();
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

		//force sync direction to current direction
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
		OnBuckledChangedHook(NetId.Invalid);
		//we can be pushed / pulled again
		PlayerScript.pushPull.ServerSetPushable(true);
		//decide if we should fall back down when unbuckled
		registerPlayer.ServerSetIsStanding(PlayerScript.playerHealth.ConsciousState == ConsciousState.CONSCIOUS);
		onUnbuckled?.Invoke();
	}

	//invoked when buckledTo changes direction, so we can update our direction
	private void OnBuckledObjectDirectionChange(Orientation newDir)
	{
		playerDirectional.FaceDirection(newDir);
	}

	//syncvar hook invoked client side when the buckledTo changes
	private void OnBuckledChangedHook(uint newBuckledTo)
	{
		//unsub if we are subbed
		if (buckledObject != NetId.Invalid)
		{
			var directionalObject = ClientScene.FindLocalObject(buckledObject).GetComponent<Directional>();
			if (directionalObject != null)
			{
				directionalObject.OnDirectionChange.RemoveListener(OnBuckledObjectDirectionChange);
			}
		}
		if (PlayerManager.LocalPlayer == gameObject)
		{
			//have to do this with a lambda otherwise the Cmd will not fire
			UIManager.AlertUI.ToggleAlertBuckled(newBuckledTo != NetId.Invalid, () => CmdUnbuckle());
		}

		buckledObject = newBuckledTo;
		//sub
		if (buckledObject != NetId.Invalid)
		{
			var directionalObject = ClientScene.FindLocalObject(buckledObject).GetComponent<Directional>();
			if (directionalObject != null)
			{
				directionalObject.OnDirectionChange.AddListener(OnBuckledObjectDirectionChange);
			}
		}
		//ensure we are in sync with server
		playerScript?.PlayerSync?.RollbackPrediction();
	}

	[Server]
	public void Cuff(HandApply interaction)
	{
		SyncCuffed(true);

		var targetStorage = interaction.TargetObject.GetComponent<ItemStorage>();
		//transfer cuffs to the special cuff slot (not exposed in UI ATM)
		Inventory.ServerTransfer(interaction.HandSlot,
			targetStorage.GetNamedItemSlot(NamedSlot.handcuffs));
		//drop hand items
		Inventory.ServerDrop(targetStorage.GetNamedItemSlot(NamedSlot.leftHand));
		Inventory.ServerDrop(targetStorage.GetNamedItemSlot(NamedSlot.rightHand));

	}

	[Server]
	private void Uncuff()
	{
		SyncCuffed(false);

		Inventory.ServerDrop(playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.handcuffs));
	}

	private void SyncCuffed(bool cuffed)
	{
		var oldCuffed = this.cuffed;
		this.cuffed = cuffed;

		if (isServer)
		{
			OnCuffChangeServer.Invoke(oldCuffed, this.cuffed);
		}
	}

	/// <summary>
	/// Called by RequestUncuffMessage after the progress bar completes
	/// Uncuffs this player after performing some legitimacy checks
	/// </summary>
	/// <param name="uncuffingPlayer"></param>
	[Server]
	public void RequestUncuff(GameObject uncuffingPlayer)
	{
		if (!cuffed || !uncuffingPlayer)
			return;

		if (!Validations.CanApply(uncuffingPlayer, gameObject, NetworkSide.Server))
			return;

		Uncuff();
	}

	/// <summary>
	/// Used for the right click action, sends a message requesting uncuffing
	/// </summary>
	public void TryUncuffThis()
	{
		RequestUncuffMessage.Send(gameObject);
	}

	/// <summary>
	/// Anything with PlayerMove can be cuffed and uncuffed. Might make sense to seperate that into its own behaviour
	/// </summary>
	/// <returns>The menu including the uncuff action if applicable, otherwise null</returns>
	public RightClickableResult GenerateRightClickOptions()
	{
		var initiator = PlayerManager.LocalPlayerScript.playerMove;

		if (IsCuffed && initiator != this)
		{
			var result = RightClickableResult.Create();
			result.AddElement("Uncuff", TryUncuffThis);
			return result;
		}

		return null;
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncCuffed(this.cuffed);
	}
}

/// <summary>
/// Cuff state changed, provides old state and new state as 1st and 2nd args
/// </summary>
public class CuffEvent : UnityEvent<bool, bool>
{

}