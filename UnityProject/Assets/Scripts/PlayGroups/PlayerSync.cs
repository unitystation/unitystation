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

		public override string ToString()
		{
			return $"[Move: {MoveNumber}, Pos: {Position}]";
		}
	}

	public struct PlayerAction
	{
		public int[] keyCodes;
	}

	public class PlayerSync : NetworkBehaviour, IPlayerSync
	{
		private bool canRegister = false;
		private HealthBehaviour healthBehaviorScript;

		//TODO: Remove the space damage coroutine when atmos is implemented
		private bool isApplyingSpaceDmg;

		private Vector2 lastDirection;

		private Matrix matrix => registerTile.Matrix;
		private Queue<PlayerAction> pendingActions;
		private Queue<PlayerAction> serverPendingActions;
		public PlayerMove playerMove;
		private PlayerScript playerScript;
		private PlayerSprites playerSprites;
		private PlayerState predictedState;

		public LayerMask matrixLayerMask;
		private RaycastHit2D[] rayHit;

		public GameObject PullingObject { get; set; }
		
		public NetworkInstanceId PullObjectID
		{
			get { return pullObjectID; }
			set { pullObjectID = value; }
		}
		//pull objects
		[SyncVar(hook = nameof(PullReset))] private NetworkInstanceId pullObjectID;

		private Vector3 pullPos;
		private RegisterTile pullRegister;
		private PushPull pushPull; //The pushpull component on this player
		private RegisterTile registerTile;
		private PlayerState serverTargetState;
		private PlayerState serverState;

		[SyncVar] private PlayerState serverStateCache; //used to sync with new players

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
				serverState = serverStateCache;
				transform.localPosition = RoundedPos(serverState.Position);
			}
			else
			{
				PlayerState state = new PlayerState {MoveNumber = 0, Position = transform.localPosition};
				serverState = state;
				predictedState = state;
			}
			yield return new WaitForSeconds(2f);

			PullReset(PullObjectID);
		}

		private void InitState()
		{
			if (isServer)
			{
				Vector3Int position = Vector3Int.RoundToInt(transform.localPosition);
				PlayerState state = new PlayerState {MoveNumber = 0, Position = position};
				serverState = state;
				serverStateCache = state;
				serverTargetState = state;
			}
		}

		//Currently used to set the pos of a player that has just been dragged by another player
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
		///     Manually set a player to a specific position
		/// </summary>
		/// <param name="pos">The new position to "teleport" player</param>
		[Server]
		public void SetPosition(Vector3 pos)
		{
			//TODO ^ check for an allowable type and other conditions to stop abuse of SetPosition
			Vector3Int roundedPos = Vector3Int.RoundToInt(pos);
			transform.localPosition = roundedPos;
			var newState = new PlayerState {MoveNumber = 0, Position = roundedPos};
			serverState = newState;
			serverTargetState = newState;
			serverStateCache = newState;
			ClearPendingServer();
			PlayerMoveMessage.SendToAll(gameObject, newState, true);
		}

		private void Start()
		{
			if (isLocalPlayer)
			{
				pendingActions = new Queue<PlayerAction>();
				UpdatePredictedState();
			}
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
				pendingActions.Enqueue(action);
//				Debug.Log($"Client requesting {action} ({pendingActions.Count} in queue)");
				UpdatePredictedState();
				RequestMoveMessage.Send(action); 
//				CmdProcessAction(action);
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
				CheckStateUpdate();
			}

			PlayerState state = DetermineState();
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

				//Check if we should still be displaying an ItemListTab and update it, if so.
				ControlTabs.CheckItemListTab();

				if (state.Position != transform.localPosition)
				{
					lastDirection = (state.Position - transform.localPosition).normalized;
				}

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

		private PlayerState DetermineState()
		{
			if ( isServer )
			{
				return isLocalPlayer ? predictedState : serverTargetState;
			}
			return isLocalPlayer ? predictedState : serverState;
		}

		[Server]
		private void CheckStateUpdate()
		{
			//checking only player movement for now
			Vector2Int localPos = Vector2Int.RoundToInt(transform.localPosition);			
			if ( localPos == Vector2Int.RoundToInt(serverState.Position) )
			{
				return;
			}
			if ( localPos == Vector2Int.RoundToInt(serverTargetState.Position) )
			{
//				Debug.Log($"Applying {serverTargetState}");
				SetServerState(serverTargetState);
				TryUpdateServerTarget();
			}
		}

		/// Tries to assign next target from queue to serverTargetState if there are any
		/// (In order to start lerping towards it)
		[Server]
		private void TryUpdateServerTarget()
		{
			if ( serverPendingActions.Count != 0 )
			{
				serverTargetState = NextState(serverTargetState, serverPendingActions.Dequeue());
				Debug.Log($"Server Updated target {serverTargetState}. {serverPendingActions.Count} pending");
			}
		}

		private void SetServerState(PlayerState newState)
		{
			serverState = newState;
			//Do not cache the position if the player is a ghost
			//or else new players will sync the deadbody with the last pos
			//of the gost:
			if ( !playerMove.isGhost )
			{
				serverStateCache = serverState;
			}
			PlayerMoveMessage.SendToAll(gameObject, serverState);
		}

		[Server]
		private bool isLerping()
		{
			return serverState.Position != transform.localPosition;
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

		private void PullObject()
		{
			pullPos = transform.localPosition - (Vector3) lastDirection;
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

		private int warnings;
		private readonly int maxWarnings = 3;

//		[Command(channel = 0)]
		private void CmdProcessAction(PlayerAction action)
		{
			if ( !PointlessMove(serverTargetState,action) )
			{
				//add action to server simulation queue
				serverPendingActions.Enqueue(action);
				if ( serverPendingActions.Count == 1 )
				{
					//Kickstart queue
					TryUpdateServerTarget();
				}
				if ( serverPendingActions.Count > 10 )
				{
					RollbackPosition();
					if ( ++warnings < maxWarnings )
					{
						InfoWindowMessage.Send(gameObject, $"This is warning {warnings} of {maxWarnings}.", "Warning");
						playerScript.playerHealth.AddBloodLoss(2);
					}
					else
					{
						InfoWindowMessage.Send(gameObject, "MWAHAHAH", "No more warnings");
						playerScript.playerNetworkActions.DropAllOnQuit();			
					}
					return;
				}
			}
			else
			{
				Debug.LogError($"Pointless move {serverTargetState}+{action.keyCodes[0]} Rolling back to {serverState}");
				RollbackPosition();
			}

			//Do not cache the position if the player is a ghost
			//or else new players will sync the deadbody with the last pos
			//of the gost:
			if (!playerMove.isGhost)
			{
				serverStateCache = serverState;
			}
		}

		private void RollbackPosition()
		{
			SetPosition(serverState.Position);
		}

		private void UpdatePredictedState()
		{
			if ( pendingActions.Count == 0 )
			{
				//plain assignment if there's nothing to predict
				predictedState = serverState;
			}
			else
			{
				//redraw prediction point from received serverState using pending actions
				var tempState = serverState;
				int curPredictedMove = predictedState.MoveNumber;
	
				foreach ( PlayerAction action in pendingActions )
				{
					//isReplay determines if this action is a replayed action for use in the prediction system
					bool isReplay = predictedState.MoveNumber <= curPredictedMove;
					tempState = NextState(tempState, action, isReplay);
				}
	
				predictedState = tempState;
				Debug.Log($"Redraw prediction: {serverState}->{predictedState}({pendingActions.Count} steps) ");
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

		public void UpdateClientState(PlayerState state)
		{
			serverState = state;
			Debug.Log($"Got server update {serverState}");
			if (pendingActions != null)
			{
				//invalidate queue if serverstate was never predicted
				bool serverAhead = serverState.MoveNumber > predictedState.MoveNumber;
				bool posMismatch = serverState.MoveNumber == predictedState.MoveNumber && serverState.Position != predictedState.Position;
				if ( serverAhead || posMismatch )
				{
					Debug.LogWarning($"serverAhead={serverAhead}, posMismatch={posMismatch}");
				}
				else
				{
					//removing actions already acknowledged by server from pending queue
					while (pendingActions.Count > 0 && pendingActions.Count > predictedState.MoveNumber - serverState.MoveNumber)
					{
						pendingActions.Dequeue();
					}
				}
				UpdatePredictedState();
			}		}

		public void ResetClientQueue()
		{
			Debug.LogError("Resetting queue as requested by server!");
			if ( pendingActions != null && pendingActions.Count > 0 )
			{
				pendingActions.Clear();
			}
		}

		private void ClearPendingServer()
		{
			Debug.LogWarning("Server queue wiped!");
			if ( serverPendingActions != null && serverPendingActions.Count > 0 )
			{
				serverPendingActions.Clear();
			}
		}

		private static Vector3 RoundedPos(Vector3 pos)
		{
			return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
		}

		private void CheckSpaceWalk()
		{
			if (matrix == null)
			{
				return;
			}


			Vector3Int pos = Vector3Int.RoundToInt(transform.localPosition);
			if (matrix.IsFloatingAt(pos))
			{
				rayHit = Physics2D.RaycastAll(transform.position, lastDirection, 1.1f, matrixLayerMask);
				for (int i = 0; i < rayHit.Length; i++){
					if(rayHit[i].collider.gameObject.layer == 24){
						playerMove.ChangeMatricies(rayHit[i].collider.gameObject.transform.parent);
					}
				}
				if (rayHit.Length > 0){
					return;
				}

				Vector3Int newGoal = Vector3Int.RoundToInt(transform.localPosition + (Vector3) lastDirection);
				serverState.Position = newGoal;
				predictedState.Position = newGoal;
			}
			if (matrix.IsSpaceAt(pos) && !healthBehaviorScript.IsDead && CustomNetworkManager.Instance._isServer
			    && !isApplyingSpaceDmg)
			{
				//Hurting people in space even if they are next to the wall
				StartCoroutine(ApplyTempSpaceDamage());
				isApplyingSpaceDmg = true;
			}
		}

		//TODO: Remove this when atmos is implemented 
		//This prevents players drifting into space indefinitely 
		private IEnumerator ApplyTempSpaceDamage()
		{
			yield return new WaitForSeconds(1f);
			healthBehaviorScript.RpcApplyDamage(null, 5, DamageType.OXY, BodyPartType.HEAD);
			//No idea why there is an isServer catch on RpcApplyDamage, but will apply on server as well in mean time:
			healthBehaviorScript.ApplyDamage(null, 5, DamageType.OXY, BodyPartType.HEAD);
			isApplyingSpaceDmg = false;
		}
	}
}