using System.Collections;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

public struct TransformState
{
    public bool Active;
    public float Speed;
    public Vector2 Impulse; //Vector2Int would be ideal, but you can't cast V2I<->V3I
    public Vector3 Position;
}

//todo: fix item freezing on click for client
//todo: investigate client item init failing
//todo: consider moving unregistering here
public class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
    public static readonly Vector3Int InvalidPos = new Vector3Int(0, 0, -100)
                                      , deOffset = new Vector3Int(-1, -1, 0);
    
    public float Speed = 2; //lerp speed

    private RegisterTile registerTile;

    private TransformState serverTransformState; //used for syncing with players, matters only for server
    private TransformState transformState;
//    private TransformState predictedTransformState;

    private Matrix matrix;

    public override void OnStartServer()
    {
        InitServerState();
        base.OnStartServer();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    public TransformState State => serverTransformState;

    private void InitServerState()
    {
        if ( isServer )
        {
            serverTransformState.Speed = Speed;
            if ( !transform.position.Equals(Vector3.zero) )
            {
                serverTransformState.Active = true;
                serverTransformState.Position =
                    Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
            }
            else
            {
                //For stuff hidden on spawn, like player equipment
                serverTransformState.Active = false;
                serverTransformState.Position = InvalidPos;
            }
//            transformState = serverTransformState;
        }
    }

    public override void OnStartClient()
    {
//        StartCoroutine(WaitForLoad());
        base.OnStartClient();
    }

    IEnumerator WaitForLoad()
    {
        yield return new WaitForEndOfFrame();
//        //hide shit until initalized
//        if ( transformState.Position == Vector3.zero )
//        {
//            updateActiveStatus();
//        }
        //idk
        yield return new WaitForSeconds(2f);
    }

    /// Manually set an item to a specific position
    [Server]
    public void SetPosition(Vector3 pos, bool notify = true) //consider adding optional lerp param
    {
        serverTransformState.Position = pos;
        if (notify)
        {
            NotifyPlayers();
        }
    }
    /// Manually set an item to a specific position
    [Server]
    public void SetState(TransformState state, bool notify = true)
    {
        serverTransformState = state;
        NotifyPlayers();
    }

    /// <summary>
    /// Dropping with some force. For space floating demo purposes.
    /// </summary>
    [Server]
    public void ForceDrop(Vector3 pos)
    {
        serverTransformState.Active = true;
        serverTransformState.Position = pos;
        //don't do impulses if item isn't going to float
        var impulse = Vector3.right;
        if ( CanDriftTo(Vector3Int.RoundToInt( serverTransformState.Position + impulse) ) )
        {
            serverTransformState.Impulse = impulse;
            serverTransformState.Speed = Random.Range(1f, 5f);
        }
        NotifyPlayers();
    }

    [Server]
    public void DisappearFromWorldServer(/*bool forceUpdate = true*/)
    {
        //be careful with forceupdate=false, it should be false only to the initiator w/preditction (if at all)
        serverTransformState.Active = false;
        serverTransformState.Position = InvalidPos;
        NotifyPlayers();
    }

    [Server]
    public void AppearAtPositionServer(Vector3 pos/*, bool forceUpdate = true*/)
    {
        serverTransformState.Active = true;
        SetPosition( pos );
    }

    /// <summary>
    /// Method to substitute transform.parent = x stuff.
    /// You shouldn't really use it anymore, 
    /// as there are high level methods that should suit your needs better.
    /// Server-only, client is not being notified
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
        transformState.Active = false;
        transformState.Position = InvalidPos;
    }

    /// <summary>
    /// Convenience method to make stuff appear at position
    /// For client prediction purposes.
    /// </summary>
    public void AppearAtPosition(Vector3 pos/*, bool forceUpdate = true*/)
    {
        transformState.Active = true;
        transformState.Position = pos;
    }

    public void UpdateClientState(TransformState newState)
    {
        //No lerp only if active state was changed
        if ( transformState.Active != newState.Active )
        {
            transform.position = newState.Position;
        }
        transformState = newState;
        if ( !transformState.Speed.Equals(0f) )
        {
            Speed = transformState.Speed;
        }

//        predictedTransformState = transformState;

        gameObject.SetActive(transformState.Active);
        
        RegisterObjects();
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
    /// sync with new player joining
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
        if ( !transformState.Active )
        {
            return;
        }

        if (isServer)
        {
            CheckSpaceDrift();
        }

//        if ( GameData.IsHeadlessServer )
//        {
//            return;
//        }
       
        if (transformState.Impulse != Vector2Int.zero)
        {
            //fixme: warning: this makes transformState.Position unrealistic!
            transformState.Position = registerTilePos() + (Vector3) transformState.Impulse.normalized; //perpetual flying
        }
        
        if ( transformState.Position != transform.position )
        {
            Lerp();
        }

        //Registering
        if ( registerTilePos() != Vector3Int.RoundToInt(transformState.Position) )
        {
            RegisterObjects();
        }
    }

    private Vector3Int registerTilePos()
    {
        return registerTile.Position - deOffset;
    }

    private void Lerp()
    {
        transform.position = Vector3.MoveTowards(transform.position, transformState.Position, Speed * Time.deltaTime);
    }

    /// <summary>
    /// trying to make drift detection serverside only, and then just send state updates to lerp to
    /// </summary>
    [Server]
    private void CheckSpaceDrift()
    {
//        var pos = Vector3Int.RoundToInt(serverTransformState.Position);
        if ( hasImpulse() && matrix != null )
        {
            Vector3Int newGoal = Vector3Int.RoundToInt(serverTransformState.Position + (Vector3) serverTransformState.Impulse.normalized);
            if ( CanDriftTo(newGoal) ) 
            {    //Spess drifting
//                if (registerTile.Position == Vector3Int.RoundToInt(serverTransformState.Position))
//                {
                    serverTransformState.Position = registerTilePos();
//                }
            }
            else //Stopping drift
            {
                serverTransformState.Impulse = Vector2.zero; //killing impulse, be aware when doing throw!
                NotifyPlayers();
                Debug.LogFormat($"{gameObject.name}: stopped floating @{serverTransformState.Position}");
            }
        }
    }

    private bool CanDriftTo(Vector3Int goal)
    {
        //FIXME: deOffset is a temp solution to this weird matrix 1,1 offset
        return matrix != null && matrix.IsEmptyAt(goal + deOffset);
    }

    private bool hasImpulse()
    {
        return !serverTransformState.Impulse.Equals(Vector2.zero);
    }
}