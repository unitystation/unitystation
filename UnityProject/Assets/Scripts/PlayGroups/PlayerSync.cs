﻿using System;
using System.Collections;
using System.Collections.Generic;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
	public struct PlayerState
	{
		public int MoveNumber;
		public Vector3 Position;
		///Direction of flying
		public Vector2 Impulse; 
		///Flag for clients to reset their queue when received 
		public bool ResetClientQueue;
		/// Flag for server to ensure that clients receive that flight update: 
		/// Only important flight updates are being sent out by server (usually start-stop only)
		[NonSerialized] public bool ImportantFlightUpdate;

		public override string ToString()
		{
			return $"{nameof(MoveNumber)}: {MoveNumber}, {nameof(Position)}: {Position}, {nameof(Impulse)}: {Impulse}, " +
			       $"reset: {ResetClientQueue}, flight: {ImportantFlightUpdate}";
		}
	}

	public struct PlayerAction
	{
		public int[] keyCodes;

		//clone of PlayerMove GetMoveDirection stuff
		//but there should be a way to see the direction of these keycodes ffs
		public Vector2Int Direction()
		{
			Vector2Int direction = Vector2Int.zero;
			for ( var i = 0; i < keyCodes.Length; i++ )
			{
				direction += GetMoveDirection(( KeyCode ) keyCodes[i]);
			}
			direction.x = Mathf.Clamp(direction.x, -1, 1);
			direction.y = Mathf.Clamp(direction.y, -1, 1);

			return direction;
		}
		private static Vector2Int GetMoveDirection(KeyCode action)
		{
			switch (action) {
				case KeyCode.Z:
				case KeyCode.W:
				case KeyCode.UpArrow:
					return Vector2Int.up;
				case KeyCode.A:
				case KeyCode.Q:
				case KeyCode.LeftArrow:
					return Vector2Int.left;
				case KeyCode.S:
				case KeyCode.DownArrow:
					return Vector2Int.down;
				case KeyCode.D:
				case KeyCode.RightArrow:
					return Vector2Int.right;
			}
			return Vector2Int.zero;
		}
	}

	public class PlayerSync : NetworkBehaviour, IPlayerSync
	{
		private bool canRegister = false;
		private HealthBehaviour healthBehaviorScript;
		public PlayerMove playerMove;
		private PlayerScript playerScript;
		private PlayerSprites playerSprites;

		//TODO: Remove the space damage coroutine when atmos is implemented
		private bool isApplyingSpaceDmg;


		private Matrix matrix => registerTile.Matrix;

		public LayerMask matrixLayerMask;
		private RaycastHit2D[] rayHit;

		public GameObject PullingObject { get; set; }
		
		public NetworkInstanceId PullObjectID
		{
			get { return pullObjectID; }
			set { pullObjectID = value; }
		}

		private Vector2 LastDirection
		{
			get { return lastDirection; }
			set
			{
				if (value != Vector2.zero)
				{
//					Debug.Log($"Setting lastDirection to {value}");
					lastDirection = value;
				}
				else
				{
					Debug.LogWarning("Attempt to set lastDirection to zero!");
				}
			}
		}

		//pull objects
		[SyncVar(hook = nameof(PullReset))] private NetworkInstanceId pullObjectID;

		private Vector3 pullPos;
		private RegisterTile pullRegister;
		private PushPull pushPull; //The pushpull component on this player
		private RegisterTile registerTile;

		//Client-only fields, don't concern server
		private PlayerState playerState;
		private PlayerState predictedState;
		private Queue<PlayerAction> pendingActions;
		private Vector2 lastDirection;
		
		///Does server claim this client is floating rn?
		private bool IsFloatingClient => playerState.Impulse != Vector2.zero;
		/// Does your client think you should be floating rn? (Regardless of what server thinks)
		private bool IsPseudoFloatingClient => predictedState.Impulse != Vector2.zero;
		/// Measure to avoid lerping back and forth in a lagspike 
		/// where player simulated entire spacewalk (start and stop) without getting server's answer yet
		private bool IsUnapprovedFloatClient = false;

		
		//Server-only fields, don't concern clients in any way
		private PlayerState serverTargetState;
		private PlayerState serverState;
		private Queue<PlayerAction> serverPendingActions;
		[SyncVar][Obsolete] private PlayerState serverStateCache; 	//todo: phase it out, it actually concerns clients
		/// Max size of serverside queue, client will be rolled back and punished if it overflows
		private readonly int maxServerQueue = 10;
		/// Amount of soft punishments before the hard one kicks in
		private readonly int maxWarnings = 3;
		private int playerWarnings;
		private Vector2 serverLastDirection;

		private bool IsFloatingServer => serverState.Impulse != Vector2.zero;
		
		/// idk if it's robust enough, but it seems to work
		private bool ServerPositionsMatch => serverTargetState.Position == serverState.Position;

		public override void OnStartServer()
		{
			PullObjectID = NetworkInstanceId.Invalid;
			InitState();
			base.OnStartServer();
		}

		public override void OnStartClient()
		{
			StartCoroutine(WaitForLoad());
			base.OnStartClient();
		}

		private IEnumerator WaitForLoad()
		{
			yield return new WaitForEndOfFrame();
			if (serverStateCache.Position != Vector3.zero && !isLocalPlayer)
			{
				playerState = serverStateCache;
				transform.localPosition = Vector3Int.RoundToInt(playerState.Position);
			}
			else
			{
				PlayerState state = new PlayerState {MoveNumber = 0, Position = transform.localPosition};
				playerState = state;
				predictedState = state;
			}
			yield return new WaitForSeconds(2f);

			PullReset(PullObjectID);
		}

		[Server]
		private void InitState()
		{
			Vector3Int position = Vector3Int.RoundToInt(transform.localPosition);
			PlayerState state = new PlayerState {MoveNumber = 0, Position = position};
			serverState = state;
			serverStateCache = state;
			serverTargetState = state;
		}


		//Currently used to set the pos of a player that has just been dragged by another player
		//Fixme: prone to exploits
		[Command]
		public void CmdSetPositionFromReset(GameObject fromObj, GameObject otherPlayer, Vector3 setPos)
		{
			if (fromObj.GetComponent<IPlayerSync>() == null) //Validation
			{
				return;
			}
			IPlayerSync otherPlayerSync = otherPlayer.GetComponent<IPlayerSync>();
			otherPlayerSync.SetPosition(setPos);
		}

		/// <summary>
		///     Manually set a player to a specific position.
		/// 	Also clears prediction queues.
		/// </summary>
		/// <param name="pos">The new position to "teleport" player</param>
		[Server]
		public void SetPosition(Vector3 pos)
		{
			ClearQueueServer();
			Vector3Int roundedPos = Vector3Int.RoundToInt(pos);
			//Note the client queue reset
			var newState = new PlayerState
			{
				MoveNumber = 0, 
				Position = roundedPos, 
				ResetClientQueue = true
			};
			serverState = newState;
			serverTargetState = newState;
			serverStateCache = newState;
			NotifyPlayers();
		}

		private void Start()
		{
			//Init pending actions queue for your local player
			if ( isLocalPlayer )
			{
				pendingActions = new Queue<PlayerAction>();
				UpdatePredictedState();
			}
			//Init pending actions queue for server 
			if ( isServer )
			{
				serverPendingActions = new Queue<PlayerAction>();
			}
			playerScript = GetComponent<PlayerScript>();
			playerSprites = GetComponent<PlayerSprites>();
			healthBehaviorScript = GetComponent<HealthBehaviour>();
			registerTile = GetComponent<RegisterTile>();
			pushPull = GetComponent<PushPull>();
		}


		private void Update()
		{
			if (isLocalPlayer && playerMove != null)
			{
				// If being pulled by another player and you try to break free
				//TODO Condition to check for handcuffs / straight jacket 
				// (probably better to adjust allowInput or something)
				if (pushPull.pulledBy != null && !playerMove.isGhost)
				{
					for (int i = 0; i < playerMove.keyCodes.Length; i++)
					{
						if (Input.GetKey(playerMove.keyCodes[i]))
						{
							playerScript.playerNetworkActions.CmdStopOtherPulling(gameObject);
						}
					}
					return;
				}
				if (predictedState.Position == transform.localPosition && !playerMove.isGhost)
				{
					DoAction();
				}
				else if (predictedState.Position == playerScript.ghost.transform.localPosition && playerMove.isGhost)
				{
					DoAction();
				}
			}

			Synchronize();
		}

		private void RegisterObjects()
		{
			//Register playerpos in matrix
			registerTile.UpdatePosition();
			//Registering objects being pulled in matrix
			if (pullRegister != null)
			{
				pullRegister.UpdatePosition();
			}
		}

		private void DoAction()
		{
			PlayerAction action = playerMove.SendAction();
			if (action.keyCodes.Length != 0 && !PointlessMove(predictedState,action))
			{
//				Debug.Log($"Client requesting {action} ({pendingActions.Count} in queue)");
		
				//experiment: not enqueueing or processing action if floating.
				//arguably it shouldn't really be like that in the future, 
				//but it's a workaround for serverstate being slightly ahead when floating
				if (!IsPseudoFloatingClient && !IsFloatingClient && !IsUnapprovedFloatClient)
				{
					pendingActions.Enqueue(action);

					LastDirection = action.Direction();
					UpdatePredictedState();
					
					//Seems like Cmds are reliable enough in this case
                    CmdProcessAction(action);
                    //				RequestMoveMessage.Send(action); 
				}
//				else
//				{
//					Debug.Log($"Client not updating PredictedState for {action}");
//				}

			}
		}

		private bool PointlessMove(PlayerState state, PlayerAction action)
		{
			return state.Position.Equals(NextState(state, action).Position);
		}

		private void Synchronize()
		{
			if (isLocalPlayer && GameData.IsHeadlessServer)
			{
				return;
			}
			if ( isServer )
			{
				CheckTargetUpdate();
			}

			PlayerState state = isLocalPlayer ? predictedState : playerState;
			if (!playerMove.isGhost)
			{
				CheckSpaceWalk();

				if (isLocalPlayer && playerMove.IsPushing || pushPull.pulledBy != null)
				{
					return;
				}

				if ( state.Position != transform.localPosition )
				{
					PlayerLerp(state);
				}

				if (isServer && !ServerPositionsMatch)
				{
					//Lerp on server if it's worth lerping 
					//and inform players if serverState reached targetState afterwards 
					ServerLerp();
					TryNotifyPlayers();
				}

				//Check if we should still be displaying an ItemListTab and update it, if so.
				ControlTabs.CheckItemListTab();

				if (PullingObject != null)
				{
					if (transform.hasChanged)
					{
						transform.hasChanged = false;
						PullObject();
					}
					else if (PullingObject.transform.localPosition != pullPos)
					{
						PullingObject.transform.localPosition = pullPos;
					}
				}

				//Registering
				if (registerTile.Position != Vector3Int.RoundToInt(state.Position))
				{
					RegisterObjects();
				}
			}
			else
			{
				if ( state.Position != playerScript.ghost.transform.localPosition )
				{
					GhostLerp(state);
				}
			}
		}

		/// try getting moves from server queue if server and target states match
		[Server]
		private void CheckTargetUpdate()
		{
			//checking only player movement for now
			if ( ServerPositionsMatch )
			{
				TryUpdateServerTarget();
			}
		}

		///	Inform players of new state when lerp is finished 
		[Server]
		private void TryNotifyPlayers()
		{
			if ( ServerPositionsMatch )
			{
//				When serverState reaches its planned destination,
//				embrace all other updates like updated moveNumber and flags
				serverState = serverTargetState;
				NotifyPlayers();
			}
		}



		/// Tries to assign next target from queue to serverTargetState if there are any
		/// (In order to start lerping towards it)
		[Server]
		private void TryUpdateServerTarget()
		{
			if (serverPendingActions.Count == 0)
			{
				return;
			}

			var nextAction = serverPendingActions.Peek();
			if ( !PointlessMove(serverTargetState, nextAction) )
			{
				if ( IsFloatingServer )
				{
					Debug.LogWarning("Server ignored move while player is floating");
					serverPendingActions.Dequeue();
					return;
				}
				PlayerState nextState = NextState(serverTargetState, serverPendingActions.Dequeue());
				serverLastDirection = Vector2Int.RoundToInt(nextState.Position - serverTargetState.Position); 
				serverTargetState = nextState;
//				Debug.Log($"Server Updated target {serverTargetState}. {serverPendingActions.Count} pending");
			} else {
				Debug.LogWarning($"Pointless move {serverTargetState}+{nextAction.keyCodes[0]} Rolling back to {serverState}");
				RollbackPosition();
			}
		}

		[Server]
		private void NotifyPlayers()
		{
//			Debug.Log($"Sending an update {serverTargetState}(real: {serverState})");
			//Do not cache the position if the player is a ghost
			//or else new players will sync the deadbody with the last pos
			//of the ghost:
			if ( !playerMove.isGhost )
			{
				serverStateCache = serverState;
			}
			//Generally not sending mid-flight updates (unless there's a sudden change of course etc.)
			if (!serverState.ImportantFlightUpdate && IsFloatingServer)
			{
				return;
			}
//				if ( serverState.ImportantFlightUpdate )
//				{
//					Debug.Log("Now simulate that flight, boy!");
//				}
			PlayerMoveMessage.SendToAll(gameObject, serverState);
			ClearStateFlags();
		}
		
		private void ClearStateFlags()
		{
			serverTargetState.ImportantFlightUpdate = false;
			serverTargetState.ResetClientQueue = false;
			serverState.ImportantFlightUpdate = false;
			serverState.ImportantFlightUpdate = false;
		}

		private void GhostLerp(PlayerState state)
		{
			playerScript.ghost.transform.localPosition =
				Vector3.MoveTowards(playerScript.ghost.transform.localPosition, state.Position, playerMove.speed * Time.deltaTime);
		}

		private void PlayerLerp(PlayerState state)
		{
			transform.localPosition = Vector3.MoveTowards(transform.localPosition, state.Position, playerMove.speed * Time.deltaTime);
		}

		[Server]
		private void ServerLerp()
		{
			serverState.Position =
				Vector3.MoveTowards(serverState.Position, serverTargetState.Position, playerMove.speed * Time.deltaTime);
		}

		private void PullObject()
		{
			pullPos = transform.localPosition - (Vector3) LastDirection;
			pullPos.z = PullingObject.transform.localPosition.z;

			Vector3Int pos = Vector3Int.RoundToInt(pullPos);
			if (matrix.IsPassableAt(pos) || matrix.ContainsAt(pos, gameObject) || matrix.ContainsAt(pos, PullingObject))
			{
				float journeyLength = Vector3.Distance(PullingObject.transform.localPosition, pullPos);
				if (journeyLength <= 2f)
				{
					PullingObject.transform.localPosition =
						Vector3.MoveTowards(PullingObject.transform.localPosition, pullPos, playerMove.speed * Time.deltaTime / journeyLength);
				}
				else
				{
					//If object gets too far away activate warp speed
					PullingObject.transform.localPosition =
						Vector3.MoveTowards(PullingObject.transform.localPosition, pullPos, playerMove.speed * Time.deltaTime * 30f);
				}
				PullingObject.BroadcastMessage("FaceDirection", playerSprites.currentDirection, SendMessageOptions.DontRequireReceiver);
			}
		}


		[Command(channel = 0)]
		private void CmdProcessAction(PlayerAction action)
		{
			//add action to server simulation queue
			serverPendingActions.Enqueue(action);
			//Rollback pos and punish player if server queue size is more than max size
			if ( serverPendingActions.Count > maxServerQueue )
			{
				RollbackPosition();
				if ( ++playerWarnings < maxWarnings )
				{
					TortureChamber.Torture(playerScript, TortureSeverity.S);
				}
				else
				{
					TortureChamber.Torture(playerScript, TortureSeverity.L);
				}
				return;
			}

			//Do not cache the position if the player is a ghost
			//or else new players will sync the deadbody with the last pos
			//of the gost:
			if (!playerMove.isGhost)
			{
				serverStateCache = serverState;
			}
		}
		/// Clear all queues and
		/// inform players of true serverState
		[Server]
		private void RollbackPosition()
		{
			SetPosition(serverState.Position);
		}

		private void UpdatePredictedState()
		{
			if ( pendingActions.Count == 0 )
			{
				//plain assignment if there's nothing to predict
				predictedState = playerState;
			}
			else
			{
				//redraw prediction point from received serverState using pending actions
				PlayerState tempState = playerState;
				int curPredictedMove = predictedState.MoveNumber;
	
				foreach ( PlayerAction action in pendingActions )
				{
					//isReplay determines if this action is a replayed action for use in the prediction system
					bool isReplay = predictedState.MoveNumber <= curPredictedMove;
					tempState = NextState(tempState, action, isReplay);
				}
	
				predictedState = tempState;
//				Debug.Log($"Redraw prediction: {playerState}->{predictedState}({pendingActions.Count} steps) ");
			}
		}

		private PlayerState NextState(PlayerState state, PlayerAction action, bool isReplay = false)
		{
			return new PlayerState {MoveNumber = state.MoveNumber + 1, Position = playerMove.GetNextPosition(Vector3Int.RoundToInt(state.Position), action, isReplay)};
		}

		public void PullReset(NetworkInstanceId netID)
		{
			PullObjectID = netID;

			transform.hasChanged = false;
			if (netID == NetworkInstanceId.Invalid)
			{
				if (PullingObject != null)
				{
					pullRegister.UpdatePosition();


					//Could be a another player
					PlayerSync otherPlayerSync = PullingObject.GetComponent<PlayerSync>();
					if (otherPlayerSync != null)
					{
						CmdSetPositionFromReset(gameObject, otherPlayerSync.gameObject, PullingObject.transform.localPosition);
					}
				}
				pullRegister = null;
				PullingObject = null;
			}
			else
			{
				PullingObject = ClientScene.FindLocalObject(netID);
				PushPull oA = PullingObject.GetComponent<PushPull>();
				pullPos = PullingObject.transform.localPosition;
				if (oA != null)
				{
					oA.pulledBy = gameObject;
				}
				pullRegister = PullingObject.GetComponent<RegisterTile>();
			}
		}

		public void ProcessAction(PlayerAction action)
		{
			CmdProcessAction(action);
		}
		
		/// Called when PlayerMoveMessage is received
		public void UpdateClientState(PlayerState state)
		{
			playerState = state;
			Debug.Log($"Got server update {playerState}");

			if (IsUnapprovedFloatClient)
			{
				if (IsFloatingClient)
				{
					Debug.Log("Your last trip got approved, yay!");
					ClearQueueClient();
					IsUnapprovedFloatClient = false;
				}
				else
				{
					Debug.LogWarning("Waiting for a sign of approval for experienced flight");
					return;
				}
			}
			//todo simplify?
			//ok, this one is hard to read.
			//point is, don't reset predicted state if it guessed impulse correctly 
			//or server is just approving old moves when you weren't flying yet
			if ( IsFloatingClient || IsPseudoFloatingClient )
			{
				if (IsFloatingClient)
				{
					LastDirection = playerState.Impulse;
				}
				//Move number check is there for the situations 
				//when server still confirms your old moves on the station while you're already in space for some time
				bool shouldReset = predictedState.Impulse != playerState.Impulse && predictedState.MoveNumber == playerState.MoveNumber;
				if ( /*!IsPseudoFloatingClient ||*/ shouldReset )
				{
					Debug.Log($"Reset predictedState {predictedState} with {playerState}");
					predictedState = playerState;
				}
				return;
			}
			if (pendingActions != null)
			{
				//invalidate queue if serverstate was never predicted
				bool serverAhead = playerState.MoveNumber > predictedState.MoveNumber;
				bool posMismatch = playerState.MoveNumber == predictedState.MoveNumber 
				                   && playerState.Position != predictedState.Position;
				if ( serverAhead || posMismatch ){
					Debug.LogWarning($"serverAhead={serverAhead}, posMismatch={posMismatch}");
					ClearQueueClient();
					predictedState = playerState;
				} else {
					//removing actions already acknowledged by server from pending queue
					while (pendingActions.Count > 0 && pendingActions.Count > predictedState.MoveNumber - playerState.MoveNumber)
					{
						pendingActions.Dequeue();
					}
				}
				UpdatePredictedState();
			}
		}

		/// Clears client pending actions queue
		public void ClearQueueClient()
		{
//			Debug.Log("Resetting queue as requested by server!");
			if ( pendingActions != null && pendingActions.Count > 0 )
			{
				pendingActions.Clear();
			}
		}

		/// Clears server pending actions queue
		private void ClearQueueServer()
		{
//			Debug.Log("Server queue wiped!");
			if ( serverPendingActions != null && serverPendingActions.Count > 0 )
			{
				serverPendingActions.Clear();
			}
		}

		/// <summary>
		/// Space walk, Push checks for client and server. Grown so large I had to separate C/S methods
		/// </summary>
		private void CheckSpaceWalk()
		{
			if (matrix == null)
			{
				return;
			}
			//Server zone
			if (isServer)
			{
				CheckSpaceWalkServer();
			}
			//Client zone
			CheckSpaceWalkClient();

		}

		///Simulate space walk by server's orders or initiate/stop them on client
		///Using predictedState for your own player and playerState for others
		private void CheckSpaceWalkClient()
		{
			PlayerState state = isLocalPlayer ? predictedState : playerState;
			Vector3Int pos = Vector3Int.RoundToInt(state.Position);
			if ( IsPseudoFloatingClient && !matrix.IsFloatingAt(pos) )
			{
				Debug.Log("Stopped clientside floating to avoid going through walls");
				//stop floating on client (if server isn't responding in time) to avoid players going through walls
				predictedState.Impulse = Vector2.zero;
				//Stopping spacewalk increases move number
				predictedState.MoveNumber++;
				if ( !IsFloatingClient && playerState.MoveNumber < predictedState.MoveNumber )
				{
					Debug.Log("Got an unapproved flight here!");
				//Client figured out that he just finished spacewalking 
				//and server is yet to approve the fact that it even started.
				//Marking as UnapprovedFloatClient 
				//to ignore further predictive movement until flight approval message is received
					IsUnapprovedFloatClient = true;
				}
			}
			if ( matrix.IsFloatingAt(pos) )
			{
				if ( state.Impulse == Vector2.zero && LastDirection != Vector2.zero )
				{
					//client initiated space dive. 						
					state.Impulse = LastDirection;
					if (isLocalPlayer)
					{
						predictedState.Impulse = state.Impulse;
					}
					else
					{
						playerState.Impulse = state.Impulse;
					}
//					Debug.Log($"Wasn't floating on client, now floating with impulse {LastDirection}. FC={IsFloatingClient},PFC={IsPseudoFloatingClient}");
				}

				//Perpetual floating sim
				if ( transform.localPosition == state.Position )
				{
					//Extending prediction by one tile if player's transform reaches previously set goal
					Vector3Int newGoal = Vector3Int.RoundToInt(state.Position + ( Vector3 ) state.Impulse);
					if (!isLocalPlayer)
					{
						playerState.Position = newGoal;
					}
					predictedState.Position = newGoal;
				}
			}
		}

		/// Ensuring server authority for space walk
		[Server]
		private void CheckSpaceWalkServer()
		{
			if ( matrix.IsFloatingAt(Vector3Int.RoundToInt(serverTargetState.Position)) )
			{
				if ( !IsFloatingServer )
				{
					//initiate floating
					//notify players that we started floating
					Push(Vector2Int.RoundToInt(serverLastDirection));
				}
				else if ( ServerPositionsMatch && !serverTargetState.ImportantFlightUpdate )
				{
					//continue floating
					serverTargetState.Position = Vector3Int.RoundToInt(serverState.Position + ( Vector3 ) serverTargetState.Impulse);
					ClearQueueServer();
				}
			}
			else if ( IsFloatingServer )
			{
				//finish floating. players will be notified as soon as serverState catches up
				serverState.Impulse = Vector2.zero;
				serverTargetState.Impulse = Vector2.zero;
				serverTargetState.ResetClientQueue = true;
				//Stopping spacewalk increases move number
				serverTargetState.MoveNumber++;
			}
			
			CheckSpaceDamage();
		}

		/// Push player in direction.
		/// Impulse should be consumed after one tile if indoors,
		/// and last indefinitely (until hit by obstacle) if you pushed someone into deep space 
		[Server]
		public void Push(Vector2Int direction)
		{
			serverState.Impulse = direction;
			serverTargetState.Impulse = direction;
			if (matrix != null)
			{
				Vector3Int pushGoal = Vector3Int.RoundToInt(serverState.Position + (Vector3) serverTargetState.Impulse);
				if (matrix.IsPassableAt(pushGoal))
				{
					Debug.Log($"Server push to {pushGoal}");
					serverTargetState.Position = pushGoal;
					serverTargetState.ImportantFlightUpdate = true;
					serverTargetState.ResetClientQueue = true;
				}
				else
				{
					serverState.Impulse = Vector2.zero;
					serverTargetState.Impulse = Vector2.zero;
				}
			}
		}

		/// Checking whether player should suffocate
		[Server]
		private void CheckSpaceDamage()
		{
			if (matrix.IsSpaceAt(Vector3Int.RoundToInt(serverState.Position))
			    && !healthBehaviorScript.IsDead && !isApplyingSpaceDmg)
			{
//				Hurting people in space even if they are next to the wall
				StartCoroutine(ApplyTempSpaceDamage());
				isApplyingSpaceDmg = true;
			}
		}

		//TODO: Remove this when atmos is implemented 
		///This prevents players drifting into space indefinitely 
		private IEnumerator ApplyTempSpaceDamage()
		{
			yield return new WaitForSeconds(1f);
//			healthBehaviorScript.RpcApplyDamage(null, 5, DamageType.OXY, BodyPartType.HEAD);
////			No idea why there is an isServer catch on RpcApplyDamage, but will apply on server as well in mean time:
//			healthBehaviorScript.ApplyDamage(null, 5, DamageType.OXY, BodyPartType.HEAD);
			isApplyingSpaceDmg = false;
		}
		
		//Visual debug
		private Vector3 size1 = Vector3.one;
		private Vector3 size2 = new Vector3(0.9f,0.9f,0.9f);
		private Vector3 size3 = new Vector3(0.8f,0.8f,0.8f);
		private Vector3 size4 = new Vector3(0.7f,0.7f,0.7f);
		private Color color1 = Color.red;
		private Color color2 = DebugTools.HexToColor("fd7c6e");
		private Color color3 = DebugTools.HexToColor("22e600");
		private Color color4 = DebugTools.HexToColor("ebfceb");

		private void OnDrawGizmos()
		{
			//server target state
			Gizmos.color = color1;
			Gizmos.DrawWireCube(serverTargetState.Position - CustomNetTransform.deOffset, size1);
			
			//actual server state
			Gizmos.color = color2;
			Gizmos.DrawWireCube(serverState.Position - CustomNetTransform.deOffset, size2);
			
			//client predicted state
			Gizmos.color = color3;
			Vector3 clientPrediction = predictedState.Position - CustomNetTransform.deOffset;
			Gizmos.DrawWireCube(clientPrediction, size3);
			GizmoUtils.DrawArrow(clientPrediction + Vector3.left / 5, predictedState.Impulse);
			GizmoUtils.DrawText(predictedState.MoveNumber.ToString(), clientPrediction + Vector3.left, 15);
			
			//client actual state
			Gizmos.color = color4;
			Vector3 clientState = playerState.Position - CustomNetTransform.deOffset;
			Gizmos.DrawWireCube(clientState, size4);
			GizmoUtils.DrawArrow(clientState + Vector3.right / 5, playerState.Impulse);
			GizmoUtils.DrawText(playerState.MoveNumber.ToString(), clientState + Vector3.right, 15);
		}
	}
}

 