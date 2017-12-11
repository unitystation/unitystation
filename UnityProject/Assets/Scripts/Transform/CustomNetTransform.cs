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


//don't forget to: turn off FOV, set ping to 200, remove ObjectBehaviour from player

//todo: consider moving unregistering here
//todo: lerping for updated stuff

/*
 * fixme client errors:
 *NullReferenceException: Object reference not set to an instance of an object
  at Tilemaps.Scripts.Matrix.GetMatrix (UnityEngine.MonoBehaviour behaviour) [0x00025] in C:\homeproj\unitystation\UnityProject\Assets\Tilemaps\Scripts\Matrix.cs:21 
  at CustomNetTransform.Start () [0x0000f] in C:\homeproj\unitystation\UnityProject\Assets\Scripts\Transform\CustomNetTransform.cs:163 
 
(Filename: C:/homeproj/unitystation/UnityProject/Assets/Tilemaps/Scripts/Matrix.cs Line: 21)

NullReferenceException: Object reference not set to an instance of an object
  at Tilemaps.Scripts.Behaviours.Objects.RegisterTile.Start () [0x00034] in C:\homeproj\unitystation\UnityProject\Assets\Tilemaps\Scripts\Behaviours\Objects\RegisterTile.cs:42 
 
(Filename: C:/homeproj/unitystation/UnityProject/Assets/Tilemaps/Scripts/Behaviours/Objects/RegisterTile.cs Line: 42)


when client sees host drop/pick stuff
NullReferenceException: Object reference not set to an instance of an object
  at TransformStateMessage+<Process>c__Iterator0.MoveNext () [0x000be] in C:\homeproj\unitystation\UnityProject\Assets\Scripts\Messages\Server\TransformStateMessage.cs:26 
  at UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) [0x00028] in C:\buildslave\unity\build\Runtime\Export\Coroutines.cs:17 
 
(Filename: C:/homeproj/unitystation/UnityProject/Assets/Scripts/Messages/Server/TransformStateMessage.cs Line: 26)
 */
public class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
    public float speed = 2; //lerp speed

    protected RegisterTile registerTile;

    private TransformState serverTransformState; //used for syncing with players
    private TransformState transformState;
    
    private Vector2 lastDirection;
    
    protected Matrix matrix;

    public override void OnStartServer()
    {
        InitState();
        base.OnStartServer();
    }

//    public override void OnStartClient()
//    {
//        StartCoroutine(WaitForLoad());
//        base.OnStartClient();
//    }
//    IEnumerator WaitForLoad()
//    {
//        yield return new WaitForEndOfFrame();
//        if ( _serverTransformStateCache.Position != Vector3.zero && !isClient )
//        {
//            transformState = _serverTransformStateCache;
//            transform.position = RoundedPos(transformState.Position);
//        }
//        else
//        {
//            transformState = new TransformState {Active = false};
//            _predictedTransformState = new TransformState {Active = false};
//        }
//        yield return new WaitForSeconds(2f);
//    }

    private void InitState()
    {
        if ( isServer )
        {
            var position = Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
            serverTransformState = new TransformState {Active = gameObject.activeInHierarchy, Position = position};
        }
    }

    /// Manually set an item to a specific position
    [Server]
    public void SetPosition(Vector3 pos, bool notify = true)
    {
//        Vector3Int roundedPos = Vector3Int.RoundToInt(pos);
//        transform.position = roundedPos; //this eliminates lerping on serverplayer
        serverTransformState = new TransformState {Active = gameObject.activeInHierarchy, Position = pos};
        if (notify)
        {
            NotifyPlayers();
        }
    }
    
    [Server]
    public void DisappearFromWorldServer(/*bool forceUpdate = true*/)
    {
        //be careful with forceupdate=false, it should be false only to the initiator w/preditction (if at all)
        serverTransformState = new TransformState {Active = false, Position = Vector3.zero};
        NotifyPlayers();
    }

    [Server]
    public void AppearAtPositionServer(Vector3 pos/*, bool forceUpdate = true*/)
    {
        serverTransformState = new TransformState {Active = true, Position = pos};
        NotifyPlayers();
    }

    /// <summary>
    /// New method to substitute transform.parent = x stuff.
    /// You shouldn't really use it a lot anymore, 
    /// as there are high level methods that should suit your needs better.
    /// Server-only for now, client is not notified
    /// </summary>
    [Server]
    public void SetParent(Transform pos)
    {
        transform.parent = pos;
    }

    /// <summary>
    /// Convenience method to make stuff disappear at position.
    /// For client prediction purposes.
    /// </summary>
    public void DisappearFromWorld(/*bool forceUpdate = true*/)
    {
        UpdateClientState(new TransformState{Active = false});
    }

    /// <summary>
    /// Convenience method to make stuff appear at position
    /// For client prediction purposes.
    /// </summary>
    public void AppearAtPosition(Vector3 pos/*, bool forceUpdate = true*/)
    {
        UpdateClientState(new TransformState {Active = true, Position = pos});
    }

    public void UpdateClientState(TransformState newState)
    {
        
        if ( transformState.Active && newState.Active )
        {
            transformState = newState;
            Lerp();
        }
        else
        {
            //no lerp
            transformState = newState;
            gameObject.SetActive(newState.Active);
            transform.position = newState.Position;
        }
    }

    /// <summary>
    /// Currently sending to everybody, but should be sent to nearby players only
    /// </summary>
    [Server]
    private void NotifyPlayers()
    {
        TransformStateMessage.SendToAll(gameObject, serverTransformState);
    }

    /// <summary>
    /// attempt to replace syncvar; this method should be called when new player joins
    /// </summary>
    /// <param name="playerGameObject"></param>
    [Server]
    public void NotifyPlayer(GameObject playerGameObject)
    {
        TransformStateMessage.Send(playerGameObject, gameObject, serverTransformState);
    }


    void Start()
    {
        registerTile = GetComponent<RegisterTile>();
        matrix = Matrix.GetMatrix(this);
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
        registerTile.UpdatePosition();
    }

    private void Synchronize()
    {
        CheckSpaceDrift();

        if ( GameData.IsHeadlessServer )
        {
            return;
        }

        //fixme??
//        _transformState = isClient ? _predictedTransformState : transformState;
        
//        transform.position = _transformState.Position;
        if ( transformState.Position != transform.position )
        {
            Lerp();
            lastDirection = ( transformState.Position - transform.position ).normalized;
        }

        //Registering
        if ( registerTile.Position != Vector3Int.RoundToInt(transformState.Position) )
        {
            RegisterObjects();
        }
    }

    private void Lerp()
    {
        transform.position = Vector3.MoveTowards(transform.position, transformState.Position, speed * Time.deltaTime);
    }


    private void CheckSpaceDrift()
    {
        var pos = Vector3Int.RoundToInt(transform.localPosition);
        if(matrix != null && matrix.IsFloatingAt(pos))
        {
            var newGoal = Vector3Int.RoundToInt(transform.localPosition + (Vector3) lastDirection);
            transformState.Position = newGoal;
        }
    }

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
//        transformState = newTransformState;
//        if ( pendingActions != null )
//        {
//            while ( pendingActions.Count > ( _predictedTransformState.MoveNumber - transformState.MoveNumber ) )
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