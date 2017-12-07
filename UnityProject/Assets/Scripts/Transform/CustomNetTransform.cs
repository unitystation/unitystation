using System.Collections;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

public struct TransformState
{
    public bool Active;
    public Vector3 Position;
}


//TODO: split into ItemTransform and ObjectTransform?
//todo: check if unregistering actually happens
public class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
    public float speed = 10; //lerp speed

    private RegisterTile registerTile;

    private TransformState _serverTransformStateCache; //used to sync with new players
    private TransformState _serverTransformState;
    private TransformState _predictedTransformState;

    //cache
    private TransformState _transformState;

    private bool canRegister = false;

    private Vector2 lastDirection;

    public override void OnStartServer()
    {
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
//        if ( _serverTransformStateCache.Position != Vector3.zero && !isClient )
//        {
//            _serverTransformState = _serverTransformStateCache;
//            transform.position = RoundedPos(_serverTransformState.Position);
//        }
//        else
//        {
            _serverTransformState = new TransformState {Active = false};
            _predictedTransformState = new TransformState {Active = false};
//        }
        yield return new WaitForSeconds(2f);
    }

    private void InitState()
    {
        if ( isServer )
        {
            var position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0);
            _serverTransformState = new TransformState {Active = gameObject.activeInHierarchy, Position = position};
            _serverTransformStateCache =
                new TransformState {Active = gameObject.activeInHierarchy, Position = position};
        }
    }

    /// Manually set an item to a specific position
    /// Internal server stuff atm, actually
    [Server]
    public void SetPosition(Vector3 pos)
    {
        Vector3 roundedPos = RoundedPos(pos);
        transform.position = roundedPos;
        _serverTransformState = new TransformState {Active = gameObject.activeInHierarchy, Position = roundedPos};
        _serverTransformStateCache = new TransformState {Active = gameObject.activeInHierarchy, Position = roundedPos};
        _predictedTransformState = new TransformState {Active = gameObject.activeInHierarchy, Position = roundedPos};
        NotifyPlayers();
    }


    /// <summary>
    /// New method to substitute transform.parent = x stuff.
    /// You shouldn't really use it a lot anymore, 
    /// as there are high level methods that should suit your needs better.
    /// Server-only for now
    /// </summary>
    [Server]
    public void SetParent(Transform pos)
    {
        transform.parent = pos;
    }

    [Server]
    public void DisappearFromWorldServer(bool forceUpdate = true)
    {
        //be careful with forceupdate=false, it should be false only to the initiator w/preditction (if at all)
        var newState = new TransformState {Active = false, Position = Vector3.zero};
        _serverTransformState = newState;
        _serverTransformStateCache = newState;
        _predictedTransformState = newState;
        NotifyPlayers();
    }

    public void DisappearFromWorld(bool forceUpdate = true)
    {
        gameObject.SetActive(false);
    }

    [Server]
    public void AppearAtPositionServer(Vector3 pos, bool forceUpdate = true)
    {
        var newState = new TransformState {Active = true, Position = pos};
        _serverTransformState = newState;
        _serverTransformStateCache = newState;
        _predictedTransformState = newState;
        NotifyPlayers();
    }

    public void AppearAtPosition(Vector3 pos, bool forceUpdate = true)
    {
        gameObject.SetActive(true);
        gameObject.transform.position = pos;
        _predictedTransformState = new TransformState {Active = true, Position = pos};
        _serverTransformState = new TransformState {Active = true, Position = pos};
    }

    /// <summary>
    /// Currently sending to everybody, but should be sent to nearby players only
    /// </summary>
    [Server]
    private void NotifyPlayers()
    {
        TransformStateMessage.SendToAll(gameObject, _serverTransformStateCache);
    }

    /// <summary>
    /// attempt to replace syncvar; this method should be called when new player joins
    /// </summary>
    /// <param name="playerGameObject"></param>
    [Server]
    public void NotifyPlayer(GameObject playerGameObject)
    {
        TransformStateMessage.Send(playerGameObject, gameObject, _serverTransformStateCache);
    }


    void Start()
    {
        registerTile = GetComponent<RegisterTile>();
    }

//managed by UpdateManager
    public override void UpdateMe()
    {
        if ( !registerTile )
        {
            registerTile = GetComponent<RegisterTile>();
        }
        Synchronize();
    }

    private void RegisterObjects()
    {
        //Register item pos in matrix
//        registerTile.UpdateTile(_transformState.Position);
    }

    private void Synchronize()
    {
        CheckSpaceDrift();

        if ( GameData.IsHeadlessServer )
        {
            return;
        }

        _transformState = isClient ? _predictedTransformState : _serverTransformState;
        transform.position = Vector3.MoveTowards(transform.position, _transformState.Position, speed * Time.deltaTime);
//        transform.position = _transformState.Position;

        if ( _transformState.Position != transform.position )
        {
            lastDirection = ( _transformState.Position - transform.position ).normalized;
        }

        //Registering
//        if ( registerTile.savedPosition != _transformState.Position )
//        {
//            RegisterObjects();
//        }
    }


    private Vector3 RoundedPos(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
    }

    private void CheckSpaceDrift()
    {
//        var nodes = Matrix.Matrix.At(transform.position, 1);
//        MatrixNode node = null;
//        for ( var i = 0; i < nodes.Count; i++ )
//        {
//            var n = nodes[i];
//            if ( !n.IsSpace() )
//            {
//                node = n;
//                break;
//            }
//        }
//        if ( node == null )
//        {
//            var newGoal = RoundedPos(transform.position) + RoundedPos(lastDirection);
//            _serverTransformState.Position = newGoal;
//            _predictedTransformState.Position = newGoal;
//        }
    }

//todo: lerping for updated stuff
//    private TransformState NextState(TransformState transformState, ItemTransformAction transformAction)
//    {
//        return new TransformState
//        {
//            MoveNumber = transformState.MoveNumber + 1,
//            Position = GetNextPosition(transformState.Position, transformAction)
//        };
//    }
//    private void OnServerStateChange(TransformState newTransformState) //ex-RPC
//    {
//        _serverTransformState = newTransformState;
//        if ( pendingActions != null )
//        {
//            while ( pendingActions.Count > ( _predictedTransformState.MoveNumber - _serverTransformState.MoveNumber ) )
//            {
//                pendingActions.Dequeue();
//            }
//            UpdatePredictedState();
//        }
//    }
//    public Vector3 GetNextPosition(Vector3 currentPosition, ItemTransformAction action)
//    {
//        var direction = GetDirection(action);
//
//        var adjustedDirection = AdjustDirection(currentPosition, direction);
//
//        if ( adjustedDirection == Vector3.zero )
//        {
//            Interact(currentPosition, direction);
//        }
//
//        return currentPosition + adjustedDirection;
//    }
//    public ItemTransformAction SendAction()
//    {
////		var actionKeys = new List<int>();
////
////		for (int i = 0; i < keyCodes.Length; i++){
//////			if (PlayerManager.LocalPlayer == gameObject && UIManager.Chat.isChatFocus)
//////				return new ItemTransformAction() { keyCodes = actionKeys.ToArray() };
////
////			if (Input.GetKey(keyCodes[i]) && allowInput) {
////				actionKeys.Add((int)keyCodes[i]);
////			}
////		}
//
//        return new ItemTransformAction() /*{ keyCodes = actionKeys.ToArray() }*/;
//    }
//    private void Interact(Vector3 currentPosition, Vector3 direction)
//    {
//        var objectActions = Matrix.Matrix.At(currentPosition + direction).GetPushPull();
//        if ( objectActions != null )
//        {
//            objectActions.TryPush(gameObject, speed, direction);
//        }
//    }
//    private Vector3 AdjustDirection(Vector3 currentPosition, Vector3 direction)
//    {
//        return direction;
//    }
}