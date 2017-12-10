using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using Tilemaps.Scripts.Behaviours.Objects;
using Matrix = Tilemaps.Scripts.Matrix;
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

    public class PlayerSync : NetworkBehaviour
    {
        public PlayerMove playerMove;
        private PlayerScript playerScript;
        private PlayerSprites playerSprites;
        private RegisterTile registerTile;
        private Queue<PlayerAction> pendingActions;
        private HealthBehaviour healthBehaviorScript;

        [SyncVar] private PlayerState serverStateCache; //used to sync with new players
        private PlayerState serverState;
        private PlayerState predictedState;

        //pull objects
        [SyncVar(hook = "PullReset")] public NetworkInstanceId pullObjectID;

        public GameObject pullingObject;
        private RegisterTile pullRegister;
        private bool canRegister = false;
        private Vector3 pullPos;
        private PushPull pushPull; //The pushpull component on this player

        private Vector2 lastDirection;

        private Matrix matrix;

        //TODO: Remove the space damage coroutine when atmos is implemented
        private bool isApplyingSpaceDmg = false;

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
            if (serverStateCache.Position != Vector3.zero && !isLocalPlayer)
            {
                serverState = serverStateCache;
                transform.localPosition = RoundedPos(serverState.Position);
            }
            else
            {
                serverState = new PlayerState() {MoveNumber = 0, Position = transform.localPosition};
                predictedState = new PlayerState() {MoveNumber = 0, Position = transform.localPosition};
            }
            yield return new WaitForSeconds(2f);

            PullReset(pullObjectID);
        }

        private void InitState()
        {
            if (isServer)
            {
                var position = Vector3Int.RoundToInt(transform.localPosition);
                serverState = new PlayerState() {MoveNumber = 0, Position = position};
                serverStateCache = new PlayerState() {MoveNumber = 0, Position = position};
            }
        }

        //Currently used to set the pos of a player that has just been dragged by another player
        [Command]
        public void CmdSetPositionFromReset(GameObject fromObj, GameObject otherPlayer, Vector3 setPos)
        {
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
            var roundedPos = Vector3Int.RoundToInt(pos);
            transform.localPosition = roundedPos;
            serverState = new PlayerState() {MoveNumber = 0, Position = roundedPos};
            serverStateCache = new PlayerState() {MoveNumber = 0, Position = roundedPos};
            predictedState = new PlayerState() {MoveNumber = 0, Position = roundedPos};
            RpcSetPosition(roundedPos);
        }

        [ClientRpc]
        private void RpcSetPosition(Vector3 pos)
        {
            predictedState = new PlayerState() {MoveNumber = 0, Position = pos};
            serverState = new PlayerState() {MoveNumber = 0, Position = pos};
            transform.localPosition = pos;
        }

        void Start()
        {
            if (isLocalPlayer)
            {
                pendingActions = new Queue<PlayerAction>();
                UpdatePredictedState();
            }
            playerScript = GetComponent<PlayerScript>();
            playerSprites = GetComponent<PlayerSprites>();
            healthBehaviorScript = GetComponent<HealthBehaviour>();
            registerTile = GetComponent<RegisterTile>();
            pushPull = GetComponent<PushPull>();
            matrix = Matrix.GetMatrix(this);
        }


        void Update()
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
            var action = playerMove.SendAction();
            if (action.keyCodes.Length != 0)
            {
                pendingActions.Enqueue(action);
                UpdatePredictedState();
                CmdAction(action);
            }
        }

        private void Synchronize()
        {
            if (isLocalPlayer && GameData.IsHeadlessServer)
                return;

            if (!playerMove.isGhost)
            {
                CheckSpaceWalk();

                if (isLocalPlayer && playerMove.IsPushing || pushPull.pulledBy != null)
                    return;

                var state = isLocalPlayer ? predictedState : serverState;
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, state.Position, playerMove.speed * Time.deltaTime);

                //Check if we should still be displaying an ItemListTab and update it, if so.
                ControlTabs.CheckItemListTab();

                if (state.Position != transform.localPosition)
                    lastDirection = (state.Position - transform.localPosition).normalized;

                if (pullingObject != null)
                {
                    if (transform.hasChanged)
                    {
                        transform.hasChanged = false;
                        PullObject();
                    }
                    else if (pullingObject.transform.localPosition != pullPos)
                    {
                        pullingObject.transform.localPosition = pullPos;
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
                var state = isLocalPlayer ? predictedState : serverState;
                playerScript.ghost.transform.localPosition = Vector3.MoveTowards(playerScript.ghost.transform.localPosition, state.Position, playerMove.speed * Time.deltaTime);
            }
        }

        private void PullObject()
        {
            pullPos = transform.localPosition - (Vector3) lastDirection;
            pullPos.z = pullingObject.transform.localPosition.z;

            var pos = Vector3Int.RoundToInt(pullPos);
            if (matrix.IsPassableAt(pos) || matrix.ContainsAt(pos, gameObject) || matrix.ContainsAt(pos, pullingObject))
            {
                float journeyLength = Vector3.Distance(pullingObject.transform.localPosition, pullPos);
                if (journeyLength <= 2f)
                {
                    pullingObject.transform.localPosition = Vector3.MoveTowards(pullingObject.transform.localPosition, pullPos, (playerMove.speed * Time.deltaTime) / journeyLength);
                }
                else
                {
                    //If object gets too far away activate warp speed
                    pullingObject.transform.localPosition = Vector3.MoveTowards(pullingObject.transform.localPosition, pullPos, (playerMove.speed * Time.deltaTime) * 30f);
                }
                pullingObject.BroadcastMessage("FaceDirection", playerSprites.currentDirection, SendMessageOptions.DontRequireReceiver);
            }
        }

        [Command(channel = 0)]
        private void CmdAction(PlayerAction action)
        {
            serverState = NextState(serverState, action);
            serverStateCache = serverState;
            RpcOnServerStateChange(serverState);
        }

        private void UpdatePredictedState()
        {
            predictedState = serverState;

            foreach (var action in pendingActions)
            {
                predictedState = NextState(predictedState, action);
            }
        }

        private PlayerState NextState(PlayerState state, PlayerAction action)
        {
            return new PlayerState()
            {
                MoveNumber = state.MoveNumber + 1,
                Position = playerMove.GetNextPosition(Vector3Int.RoundToInt(state.Position), action)
            };
        }

        public void PullReset(NetworkInstanceId netID)
        {
            pullObjectID = netID;

            transform.hasChanged = false;
            if (netID == NetworkInstanceId.Invalid)
            {
                if (pullingObject != null)
                {
                    pullRegister.UpdatePosition();
                    EditModeControl eM = pullingObject.GetComponent<EditModeControl>();
                    if (eM != null)
                    {
                        //This is for objects with editmodecontrol on them
                        eM.Snap();
                    }
                    else
                    {
                        //Could be a another player
                        PlayerSync otherPlayerSync = pullingObject.GetComponent<PlayerSync>();
                        if (otherPlayerSync != null)
                            CmdSetPositionFromReset(gameObject, otherPlayerSync.gameObject, pullingObject.transform.localPosition);
                    }
                }
                pullRegister = null;
                pullingObject = null;
            }
            else
            {
                pullingObject = ClientScene.FindLocalObject(netID);
                PushPull oA = pullingObject.GetComponent<PushPull>();
                pullPos = pullingObject.transform.localPosition;
                if (oA != null)
                {
                    oA.pulledBy = gameObject;
                }
                pullRegister = pullingObject.GetComponent<RegisterTile>();
            }
        }

        [ClientRpc(channel = 0)]
        private void RpcOnServerStateChange(PlayerState newState)
        {
            serverState = newState;
            if (pendingActions != null)
            {
                while (pendingActions.Count > 0 && pendingActions.Count > (predictedState.MoveNumber - serverState.MoveNumber))
                {
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
            var pos = Vector3Int.RoundToInt(transform.localPosition);
            if (matrix != null && matrix.IsFloatingAt(pos))
            {
                var newGoal = Vector3Int.RoundToInt(transform.localPosition + (Vector3) lastDirection);
                serverState.Position = newGoal;
                predictedState.Position = newGoal;
                if (!healthBehaviorScript.IsDead && CustomNetworkManager.Instance._isServer
                    && !isApplyingSpaceDmg)
                {
                    StartCoroutine(ApplyTempSpaceDamage());
                    isApplyingSpaceDmg = true;
                }
            }
        }

        //TODO: Remove this when atmos is implemented 
        //This prevents players drifting into space indefinitely 
        IEnumerator ApplyTempSpaceDamage()
        {
            yield return new WaitForSeconds(1f);
            healthBehaviorScript.RpcApplyDamage("SPESS", 5, DamageType.OXY, BodyPartType.HEAD);
            //No idea why there is an isServer catch on RpcApplyDamage, but will apply on server as well in mean time:
            healthBehaviorScript.ApplyDamage("SPESS", 5, DamageType.OXY, BodyPartType.HEAD);
            isApplyingSpaceDmg = false;
        }
    }
}