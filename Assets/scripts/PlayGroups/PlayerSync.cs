﻿using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine;
using Matrix;
using UI;

namespace PlayGroup
{

	public struct PlayerState
	{
		public int MoveNumber;
		public Vector3 Position;
	}

	public struct PlayerAction
	{
		public int[] keyCodes;
	}

	public class PlayerSync : ManagedNetworkBehaviour //see UpdateManager
	{

		public PlayerMove playerMove;
		private PlayerScript playerScript;
		private PlayerSprites playerSprites;
		private RegisterTile registerTile;
		private Queue<PlayerAction> pendingActions;

		[SyncVar]
		private PlayerState serverStateCache; //used to sync with new players
		private PlayerState serverState;
		private PlayerState predictedState;

		//cache
		private PlayerState state;
		//pull objects
		[SyncVar(hook = "PullReset")]
		public NetworkInstanceId pullObjectID;
		public GameObject pullingObject;
		private RegisterTile pullRegister;
		private bool canRegister = false;
		private Vector3 pullPos;
		private PushPull pushPull; //The pushpull component on this player
		
		private Vector2 lastDirection;

		public override void OnStartServer()
		{
			pullObjectID = NetworkInstanceId.Invalid;
			InitState();
			base.OnStartServer();
		}
		public override void OnStartClient()
		{
			StartCoroutine(WaitForLoad());
			base.OnStartClient();
		}

		IEnumerator WaitForLoad()
		{
			yield return new WaitForEndOfFrame();
			if (serverStateCache.Position != Vector3.zero && !isLocalPlayer) {
				serverState = serverStateCache;
				transform.position = RoundedPos(serverState.Position);
			} else {
				serverState = new PlayerState() { MoveNumber = 0, Position = RoundedPos(transform.position) };
				predictedState = new PlayerState() { MoveNumber = 0, Position = RoundedPos(transform.position) };
			}
			yield return new WaitForSeconds(2f);

			PullReset(pullObjectID);
		}

		private void InitState()
		{
			if (isServer) {
				var position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0);
				serverState = new PlayerState() { MoveNumber = 0, Position = position };
				serverStateCache = new PlayerState() { MoveNumber = 0, Position = position };
			}
		}

		//Currently used to set the pos of a player that has just been dragged by another player
		[Command]
		public void CmdSetPositionFromReset(GameObject fromObj, GameObject otherPlayer, Vector3 setPos){
			if (fromObj.GetComponent<PlayerSync>() == null) //Validation
				return;

			PlayerSync otherPlayerSync = otherPlayer.GetComponent<PlayerSync>();
			otherPlayerSync.SetPosition(setPos);
		}
		/// <summary>
		/// Manually set a player to a specific position
		/// </summary>
		/// <param name="pos">The new position to "teleport" player</param>
		[Server]
		public void SetPosition(Vector3 pos)
		{
			//TODO ^ check for an allowable type and other conditions to stop abuse of SetPosition
			Vector3 roundedPos = RoundedPos(pos);
			transform.position = roundedPos;
			serverState = new PlayerState() { MoveNumber = 0, Position = roundedPos };
			serverStateCache = new PlayerState() { MoveNumber = 0, Position = roundedPos };
			predictedState = new PlayerState() { MoveNumber = 0, Position = roundedPos };
			RpcSetPosition(roundedPos);
		}

		[ClientRpc]
		private void RpcSetPosition(Vector3 pos)
		{
		    predictedState = new PlayerState() { MoveNumber = 0, Position = pos };
			serverState = new PlayerState() { MoveNumber = 0, Position = pos };
			transform.position = pos;
		}
		
		void Start()
		{
			if (isLocalPlayer) {
				pendingActions = new Queue<PlayerAction>();
				UpdatePredictedState();
			}
			playerScript = GetComponent<PlayerScript>();
			playerSprites = GetComponent<PlayerSprites>();
			registerTile = GetComponent<RegisterTile>();
			pushPull = GetComponent<PushPull>();
		}

		//managed by UpdateManager
		public override void UpdateMe()
		{
			if (isLocalPlayer && playerMove != null)
			{
				// If being pulled by another player and you try to break free
				//TODO Condition to check for handcuffs / straight jacket 
				// (probably better to adjust allowInput or something)
				if (pushPull.pulledBy != null && !playerMove.isGhost) {
					for (int i = 0; i < playerMove.keyCodes.Length; i++){
						if(Input.GetKey(playerMove.keyCodes[i])){
							playerScript.playerNetworkActions.CmdStopOtherPulling(gameObject);
						}
					}
					return;
				}
				if (predictedState.Position == transform.position && !playerMove.isGhost)
				{
					DoAction();
				}
				else if (predictedState.Position == playerScript.ghost.transform.position && playerMove.isGhost)
				{
					DoAction();
				}
			}

			Synchronize();
		}

		private void RegisterObjects()
		{
			//Register playerpos in matrix
			registerTile.UpdateTile(state.Position);
			//Registering objects being pulled in matrix
			if (pullRegister != null) {
				Vector3 pos = state.Position - (Vector3)playerSprites.currentDirection;
				pullRegister.UpdateTile(pos);
			}
		}

		private void DoAction()
		{
			var action = playerMove.SendAction();
			if (action.keyCodes.Length != 0) {
				pendingActions.Enqueue(action);
				UpdatePredictedState();
				CmdAction(action);
			}
		}

		private void Synchronize()
		{
			CheckSpaceWalk();

			if (isLocalPlayer && GameData.IsHeadlessServer)
				return;

			if (!playerMove.isGhost) {
				if (isLocalPlayer && playerMove.isPushing || pushPull.pulledBy != null)
					return;

				state = isLocalPlayer ? predictedState : serverState;
				transform.position = Vector3.MoveTowards(transform.position, state.Position, playerMove.speed * Time.deltaTime);

				ControlTabs.CheckItemListTab();

				if (state.Position != transform.position)
					lastDirection = (state.Position - transform.position).normalized;

				if (pullingObject != null) {
					if (transform.hasChanged) {
						transform.hasChanged = false;
						PullObject();
					} else if (pullingObject.transform.position != pullPos) {
						pullingObject.transform.position = pullPos;
					}
				}

				//Registering
				if (registerTile.savedPosition != state.Position) {
					RegisterObjects();
				}
			} else {
				var state = isLocalPlayer ? predictedState : serverState;
				playerScript.ghost.transform.position = Vector3.MoveTowards(playerScript.ghost.transform.position, state.Position, playerMove.speed * Time.deltaTime);
			}
		}

		private void PullObject()
		{
			pullPos = transform.position - (Vector3) lastDirection;
			pullPos.z = pullingObject.transform.position.z;
			if (Matrix.Matrix.At(pullPos).IsPassable() ||
				Matrix.Matrix.At(pullPos).ContainsTile(pullingObject) ||
				Matrix.Matrix.At(pullPos).ContainsTile(gameObject)) {
				float journeyLength = Vector3.Distance(pullingObject.transform.position, pullPos);
				if (journeyLength <= 2f) {
					pullingObject.transform.position = Vector3.MoveTowards(pullingObject.transform.position, pullPos, (playerMove.speed * Time.deltaTime) / journeyLength);
				} else {
					//If object gets too far away activate warp speed
					pullingObject.transform.position = Vector3.MoveTowards(pullingObject.transform.position, pullPos, (playerMove.speed * Time.deltaTime) * 30f);
				}
				pullingObject.BroadcastMessage("FaceDirection", playerSprites.currentDirection, SendMessageOptions.DontRequireReceiver);
			}
		}

		[Command(channel=0)]
		private void CmdAction(PlayerAction action)
		{
			serverState = NextState(serverState, action);
			serverStateCache = serverState;
			RpcOnServerStateChange(serverState);

		}

		private void UpdatePredictedState()
		{
			predictedState = serverState;

			foreach (var action in pendingActions) {
				predictedState = NextState(predictedState, action);
			}
		}

		private PlayerState NextState(PlayerState state, PlayerAction action)
		{

			return new PlayerState() {
				MoveNumber = state.MoveNumber + 1,
				Position = playerMove.GetNextPosition(state.Position, action)
			};
		}

		public void PullReset(NetworkInstanceId netID)
		{
			pullObjectID = netID;
			if (netID == null)
				netID = NetworkInstanceId.Invalid;

			transform.hasChanged = false;
			if (netID == NetworkInstanceId.Invalid) {
				if (pullingObject != null) {
					pullRegister.UpdateTile(pullingObject.transform.position);
					EditModeControl eM = pullingObject.GetComponent<EditModeControl>();
					if (eM != null) {
						//This is for objects with editmodecontrol on them
						eM.Snap();
					} else {
						//Could be a another player
						PlayerSync otherPlayerSync = pullingObject.GetComponent<PlayerSync>();
						if (otherPlayerSync != null)
							CmdSetPositionFromReset(gameObject, otherPlayerSync.gameObject, pullingObject.transform.position);
					}
				}
				pullRegister = null;
				pullingObject = null;
			} else {
				pullingObject = ClientScene.FindLocalObject(netID);
				PushPull oA = pullingObject.GetComponent<PushPull>();
				pullPos = pullingObject.transform.position;
				if (oA != null) {
					oA.pulledBy = gameObject;
				}
				pullRegister = pullingObject.GetComponent<RegisterTile>();
			}
		}

		[ClientRpc(channel=0)]
		private void RpcOnServerStateChange(PlayerState newState)
		{
			serverState = newState;
			if (pendingActions != null) {
				while (pendingActions.Count > (predictedState.MoveNumber - serverState.MoveNumber)) {
					pendingActions.Dequeue();
				}
				UpdatePredictedState();
			}
		}

		private Vector3 RoundedPos(Vector3 pos)
		{
			return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
		}

		private void CheckSpaceWalk()
		{
			var nodes = Matrix.Matrix.At(transform.position, 1);
			var node = nodes.FirstOrDefault(n => !n.IsEmpty());
			if (node == null)
			{
				var newGoal = RoundedPos(transform.position) + RoundedPos(lastDirection);
				serverState.Position = newGoal;
				predictedState.Position = newGoal;
			}
		}
	}
}