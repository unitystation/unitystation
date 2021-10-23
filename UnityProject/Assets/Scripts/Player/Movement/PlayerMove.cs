using System;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Messages.Client.Interaction;
using Mirror;
using Objects;
using UI;
using UI.Action;
using UnityEngine;
using UnityEngine.Events;

namespace Player.Movement
{
	/// <summary>
	///     ** Now all movement input keys are sent to PlayerSync.Client
	/// 	** PlayerMove may become obsolete in the future
	///     It also changes the sprite direction and
	///     handles interaction with objects that can
	///     be walked into it.
	/// </summary>
	public class PlayerMove : NetworkBehaviour, IRightClickable, IServerSpawn, IActionGUI, ICheckedInteractable<ContextMenuApply>, RegisterPlayer.IControlPlayerState
	{
		public PlayerScript PlayerScript { get; private set; }

		public bool diagonalMovement;

		[SyncVar] public bool allowInput = true;

		// netid of the game object we are buckled to, NetId.Empty if not buckled
		[SyncVar(hook = nameof(SyncBuckledObjectNetId))]
		private uint buckledObjectNetId = NetId.Empty;

		/// <summary>
		/// Object this player is buckled to (if buckled). Null if not buckled.
		/// </summary>
		public GameObject BuckledObject { get; private set; }

		// cached for fast access

		// callback invoked when we are unbuckled.
		private Action onUnbuckled;

		/// <summary>
		/// Whether character is buckled to a chair
		/// </summary>
		public bool IsBuckled => BuckledObject != null;

		/// <summary>
		/// Whether the character is restrained with handcuffs (or similar)
		/// </summary>
		[field: SyncVar(hook = nameof(SyncCuffed))]
		public bool IsCuffed { get; private set; }

		/// <summary>
		/// Whether the character is trapped in a closet (or similar)
		/// </summary>
		public bool IsTrapped = false;

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
		// starts true because all players spawn with help intent.
		private bool isHelpIntentServer = true;


		[SerializeField]
		private ActionData actionData = null;
		public ActionData ActionData => actionData;

		/// <summary>
		/// Whether this player meets all the conditions for being swapped with (being the swapee).
		/// </summary>
		public bool IsSwappable => CanSwap();

		private bool CanSwap()
		{
			bool canSwap;
			if (isLocalPlayer && !isServer)
			{
				if (PlayerScript.pushPull == null)
				{
					// Is a ghost
					canSwap = false;
				}
				else
				{
					// locally predict
					canSwap = UIManager.CurrentIntent == Intent.Help
					          && !PlayerScript.pushPull.IsPullingSomething;
				}
			}
			else
			{
				// rely on server synced value
				canSwap = isSwappable;
			}

			return canSwap
			       // don't swap with ghosts
			       && PlayerScript.IsGhost == false
			       // pass through players if we can
			       && registerPlayer.IsPassable(isServer) == false
			       // can't swap with buckled players, they're strapped down
			       && IsBuckled == false;
		}

		private readonly List<MoveAction> moveActionList = new List<MoveAction>();

		public MoveAction[] moveList =
		{
			MoveAction.MoveUp, MoveAction.MoveLeft, MoveAction.MoveDown, MoveAction.MoveRight
		};

		[NonSerialized]
		public PlayerNetworkActions pna;

		[SyncVar(hook = nameof(SyncRunSpeed))] public float RunSpeed;
		[SyncVar(hook = nameof(SyncWalkSpeed))] public float WalkSpeed;
		[SyncVar(hook = nameof(SyncCrawlingSpeed))] public float CrawlSpeed;

		/// <summary>
		/// Player will fall when pushed with such speed
		/// </summary>
		public float PushFallSpeed = 10;

		private RegisterPlayer registerPlayer;
		private Matrix Matrix => registerPlayer.Matrix;
		private Directional playerDirectional;

		private void Awake()
		{
			PlayerScript = GetComponent<PlayerScript>();

			playerDirectional = gameObject.GetComponent<Directional>();

			registerPlayer = GetComponent<RegisterPlayer>();
			pna = gameObject.GetComponent<PlayerNetworkActions>();
			PlayerScript.registerTile.AddStatus(this);
			// Aren't these set up with sync vars? Why are they set like this?
			// They don't appear to ever get synced either.
			if (PlayerScript.IsGhost)
			{
				return;
			}

			RunSpeed = 1;
			WalkSpeed = 1;
			CrawlSpeed = 0f;
		}

		public override void OnStartClient()
		{
			SyncCuffed(IsCuffed, this.IsCuffed);
			base.OnStartClient();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			// when pulling status changes, re-check whether client needs to be told if
			// this is swappable.
			if (PlayerScript.pushPull != null)
			{
				PlayerScript.pushPull.OnPullingSomethingChangedServer.AddListener(ServerUpdateIsSwappable);
			}

			ServerUpdateIsSwappable();
		}

		public Vector3Int GetNextPosition(Vector3Int currentPosition, PlayerAction action, bool isReplay,
			Matrix curMatrix = null)
		{
			if (curMatrix == false)
			{
				curMatrix = Matrix;
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

			if (matrixInfo?.MatrixMove)
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

		bool RegisterPlayer.IControlPlayerState.AllowChange(bool rest)
		{
			return BuckledObject == null;
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
				                " can only be used on objects with a Net ID. Ensure this object has one.", Category.Movement);
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
			// can't push/pull when buckled in, break if we are pulled / pulling
			// sinform the puller
			if (PlayerScript.pushPull.PulledBy != null)
			{
				PlayerScript.pushPull.PulledBy.ServerStopPulling();
			}

			PlayerScript.pushPull.StopFollowing();
			PlayerScript.pushPull.ServerStopPulling();
			PlayerScript.pushPull.ServerSetPushable(false);
			onUnbuckled = unbuckledAction;

			// sync position to ensure they buckle to the correct spot
			PlayerScript.PlayerSync.SetPosition(toObject.TileWorldPosition().To3Int());

			// set direction if toObject has a direction
			var directionalObject = toObject.GetComponent<Directional>();
			if (directionalObject != null)
			{
				playerDirectional.FaceDirection(directionalObject.CurrentDirection);
			}
			else
			{
				playerDirectional.FaceDirection(playerDirectional.CurrentDirection);
			}

			// force sync direction to current direction (If it is a real player and not a NPC)
			if (PlayerScript.connectionToClient != null)
				playerDirectional.TargetForceSyncDirection(PlayerScript.connectionToClient);
		}

		/// <summary>
		/// Unbuckle the player when they are currently buckled..
		/// </summary>
		[Command]
		public void CmdUnbuckle()
		{
			if (IsCuffed)
			{
				Chat.AddActionMsgToChat(
					PlayerScript.gameObject,
					"You're trying to unbuckle yourself from the chair! (this will take some time...)",
					PlayerScript.name + " is trying to unbuckle themself from the chair!"
				);
				StandardProgressAction.Create(
					new StandardProgressActionConfig(StandardProgressActionType.Unbuckle),
					Unbuckle
				).ServerStartProgress(
					BuckledObject.RegisterTile(),
					BuckledObject.GetComponent<BuckleInteract>().ResistTime,
					PlayerScript.gameObject
				);
				return;
			}
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
			// we can be pushed / pulled again
			PlayerScript.pushPull.ServerSetPushable(true);
			// decide if we should fall back down when unbuckled
			registerPlayer.ServerSetIsStanding(PlayerScript.playerHealth.ConsciousState == ConsciousState.CONSCIOUS);
			onUnbuckled?.Invoke();

			if (previouslyBuckledTo == null) return;

			var integrityBuckledObject = previouslyBuckledTo.GetComponent<Integrity>();
			if(integrityBuckledObject != null) integrityBuckledObject.OnServerDespawnEvent -= Unbuckle;

			// we are unbuckled but still will drift with the object.
			var buckledCNT = previouslyBuckledTo.GetComponent<CustomNetTransform>();
			if (buckledCNT.IsFloatingServer)
			{
				PlayerScript.PlayerSync.NewtonianMove(buckledCNT.ServerImpulse.NormalizeToInt(), buckledCNT.SpeedServer);
			}
			else
			{
				// stop in place because our object wasn't moving either.
				PlayerScript.PlayerSync.Stop();
			}
		}

		// invoked when buckledTo changes direction, so we can update our direction
		private void OnBuckledObjectDirectionChange(Orientation newDir)
		{
			if (playerDirectional == null)
			{
				playerDirectional = gameObject.GetComponent<Directional>();
			}
			playerDirectional.FaceDirection(newDir);
		}

		// syncvar hook invoked client side when the buckledTo changes
		private void SyncBuckledObjectNetId(uint oldBuckledTo, uint newBuckledTo)
		{
			// unsub if we are subbed
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
			BuckledObject = NetworkUtils.FindObjectOrNull(buckledObjectNetId);

			// sub
			if (BuckledObject != null)
			{
				var directionalObject = BuckledObject.GetComponent<Directional>();
				if (directionalObject != null)
				{
					directionalObject.OnDirectionChange.AddListener(OnBuckledObjectDirectionChange);
				}
			}

			// ensure we are in sync with server
			PlayerScript.OrNull()?.PlayerSync.OrNull()?.RollbackPrediction();
		}

		private readonly HashSet<IMovementEffect> movementAffects = new HashSet<IMovementEffect>();

		[Server]
		public void AddModifier( IMovementEffect modifier)
		{
			movementAffects.Add(modifier);
			UpdateSpeeds();
		}

		[Server]
		public void RemoveModifier( IMovementEffect modifier)
		{
			movementAffects.Remove(modifier);
			UpdateSpeeds();
		}

		public void UpdateSpeeds()
		{
			float newRunSpeed = 0;
			float newWalkSpeed = 0;
			float newCrawlSpeed = 0;
			foreach (var movementAffect in movementAffects)
			{
				newRunSpeed += movementAffect.RunningSpeedModifier;
				newWalkSpeed += movementAffect.WalkingSpeedModifier;
				newCrawlSpeed += movementAffect.CrawlingSpeedModifier;
			}

			RunSpeed = Mathf.Clamp(newRunSpeed, 0, float.MaxValue);
			WalkSpeed = Mathf.Clamp(newWalkSpeed, 0, float.MaxValue);
			CrawlSpeed = Mathf.Clamp(newCrawlSpeed, 0, float.MaxValue);
		}

		private void SyncRunSpeed(float oldSpeed, float newSpeed)
		{
			RunSpeed = newSpeed;
		}

		private void SyncWalkSpeed(float oldSpeed, float newSpeed)
		{
			WalkSpeed = newSpeed;
		}

		private void SyncCrawlingSpeed(float oldSpeed, float newSpeed)
		{
			CrawlSpeed = newSpeed;
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
			PlayerHealthV2 playerHealth = PlayerScript.playerHealth;

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
			SyncCuffed(IsCuffed, this.IsCuffed);
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
			return DefaultWillInteract.Default(interaction, side) && IsCuffed;
		}

		/// <summary>
		/// Handles the interaction request for uncuffing serverside
		/// </summary>
		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			var handcuffSlots = interaction.TargetObject.GetComponent<DynamicItemStorage>().OrNull()?.GetNamedItemSlots(NamedSlot.handcuffs)
				.Where(x => x.IsEmpty == false).ToList();

			if (handcuffSlots == null) return;

			//Somehow has no cuffs but has cuffed effect, force uncuff
			if (handcuffSlots.Count == 0)
			{
				Uncuff();
				return;
			}

			foreach (var handcuffSlot in handcuffSlots)
			{
				var restraint = handcuffSlot.Item.GetComponent<Restraint>();
				if (restraint == null) continue;

				var progressConfig = new StandardProgressActionConfig(StandardProgressActionType.Uncuff);
				StandardProgressAction.Create(progressConfig, Uncuff)
					.ServerStartProgress(interaction.TargetObject.RegisterTile(),
						restraint.RemoveTime * (handcuffSlots.Count / 2f), interaction.Performer);

				//Only need to do it once
				break;
			}
		}

		[Server]
		public void Cuff(HandApply interaction)
		{
			SyncCuffed(IsCuffed, true);

			var targetStorage = interaction.TargetObject.GetComponent<DynamicItemStorage>();

			//transfer cuffs to the special cuff slot

			foreach (var handcuffSlot in targetStorage.GetNamedItemSlots(NamedSlot.handcuffs))
			{
				Inventory.ServerTransfer(interaction.HandSlot, handcuffSlot);
				break;
			}

			//drop hand items
			foreach (var itemSlot in targetStorage.GetNamedItemSlots(NamedSlot.leftHand))
			{
				Inventory.ServerDrop(itemSlot);
			}

			foreach (var itemSlot in targetStorage.GetNamedItemSlots(NamedSlot.rightHand))
			{
				Inventory.ServerDrop(itemSlot);
			}

			if (connectionToClient != null)
			{
				TargetPlayerUIHandCuffToggle(connectionToClient, true);
			}
		}

		[TargetRpc]
		private void TargetPlayerUIHandCuffToggle(NetworkConnection target, bool HideState)
		{
			HandsController.Instance.HideHands(HideState);
		}

		/// <summary>
		/// Request a ContextMenuApply interaction if you have not done your own validation.
		/// Calling this clientside will break your client.
		/// </summary>
		[Server]
		public void Uncuff()
		{
			SyncCuffed(IsCuffed, false);
			foreach (var itemSlot in PlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.handcuffs))
			{
				Inventory.ServerDrop(itemSlot);
			}

			//Connection will be null when uncuffing a disconnected player
			if(connectionToClient == null) return;

			TargetPlayerUIHandCuffToggle(connectionToClient, false);
		}

		private void SyncCuffed(bool wasCuffed, bool cuffed)
		{
			var oldCuffed = this.IsCuffed;
			this.IsCuffed = cuffed;

			if (isServer)
			{
				OnCuffChangeServer.Invoke(oldCuffed, this.IsCuffed);
			}
		}

		#endregion Cuffing
	}

	/// <summary>
	/// Cuff state changed, provides old state and new state as 1st and 2nd args
	/// </summary>
	public class CuffEvent : UnityEvent<bool, bool> { }
}