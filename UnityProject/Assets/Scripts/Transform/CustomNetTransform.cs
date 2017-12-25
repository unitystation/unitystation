using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;
// ReSharper disable CompareOfFloatsByEqualityOperator

public struct TransformState
{
    public bool Active;
    public float Speed;
    public Vector2 Impulse; //Should always be normalized in calculations
    public Vector3 Position;
}

public class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
    public static readonly Vector3Int InvalidPos = new Vector3Int(0, 0, -100)
                                      , deOffset = new Vector3Int(-1, -1, 0);
    
    public float SpeedMultiplier = 1; //Multiplier for flying/lerping speed, could indicate weight, for example

    private RegisterTile registerTile;

    private TransformState serverTransformState; //used for syncing with players, matters only for server
    private TransformState transformState;

    private Matrix matrix;

    public bool IsFloating()
    {
        if ( isServer )
        {
            return serverTransformState.Impulse != Vector2.zero && serverTransformState.Speed != 0f;
        }
        return transformState.Impulse != Vector2.zero && transformState.Speed != 0f;
    }

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
            serverTransformState.Speed = SpeedMultiplier;
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
        }
    }

    /// Manually set an item to a specific position
    [Server]
    public void SetPosition(Vector3 pos, bool notify = true)
    {
        serverTransformState.Position = pos;
        if (notify)
        {
            NotifyPlayers();
        }
    }
    
//    /// Overwrite server state with a completely new one. (Are you sure you really want that?)
//    [Server]
//    public void SetState(TransformState state)
//    {
//        serverTransformState = state;
//        NotifyPlayers();
//    }

    /// <summary>
    /// Dropping with some force. For space floating demo purposes.
    /// </summary>
    [Server]
    public void ForceDrop(Vector3 pos)
    {
        serverTransformState.Active = true;
        serverTransformState.Position = pos;
        var impulse = (Vector3) Random.insideUnitCircle;
        impulse.Normalize();
        //don't do impulses if item isn't going to float
        if ( CanDriftTo(Vector3Int.RoundToInt( serverTransformState.Position + impulse) ) )
        {
            serverTransformState.Impulse = impulse;
            serverTransformState.Speed = Random.Range(1f, 2f);
        }
        NotifyPlayers();
    }

    [Server]
    public void DisappearFromWorldServer()
    {
        serverTransformState.Active = false;
        serverTransformState.Position = InvalidPos;
        NotifyPlayers();
    }

    [Server]
    public void AppearAtPositionServer(Vector3 pos)
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
    /// For CLIENT prediction purposes.
    /// </summary>
    public void DisappearFromWorld()
    {
        transformState.Active = false;
        transformState.Position = InvalidPos;
        updateActiveStatus();        
    }

    /// <summary>
    /// Convenience method to make stuff appear at position
    /// For CLIENT prediction purposes.
    /// </summary>
    public void AppearAtPosition(Vector3 pos)
    {
        transformState.Active = true;
        transformState.Position = pos;
        updateActiveStatus();        
    }

    public void UpdateClientState(TransformState newState)
    {
        //Don't lerp (instantly change pos) if active state was changed or speed is zero
        if ( transformState.Active != newState.Active || newState.Speed == 0 )
        {
            transform.position = newState.Position;
        }
        transformState = newState;

        updateActiveStatus();        
    }

    private void updateActiveStatus()
    {
        if ( transformState.Active )
        {
            RegisterObjects();
        }
        else
        {
            registerTile.Unregister();
        }
        //todo: Consider moving VisibleBehaviour functionality to CNT. Currently VB doesn't allow predictive object hiding, for example. 
        VisibleBehaviour vb = gameObject.GetComponent<VisibleBehaviour>();
        vb.visibleState = transformState.Active; //this a syncvar -> only works for server
        vb.UpdateState(transformState.Active); //Hack to make VB work with clientside prediction
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
       
        if (IsFloating())
        {
            transformState.Position = registerTilePos() + (Vector3) transformState.Impulse.normalized; //predictive perpetual flying
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
        transform.position = Vector3.MoveTowards(transform.position, transformState.Position, (transformState.Speed * SpeedMultiplier) * Time.deltaTime);
    }

    /// <summary>
    /// Space drift detection is serverside only
    /// </summary>
    [Server]
    private void CheckSpaceDrift()
    {
        if ( IsFloating() && matrix != null )
        {
            Vector3Int newGoal = Vector3Int.RoundToInt(serverTransformState.Position + (Vector3) serverTransformState.Impulse.normalized);
            if ( CanDriftTo(newGoal) ) 
            {    //Spess drifting
                serverTransformState.Position = registerTilePos();
            }
            else //Stopping drift
            {
                serverTransformState.Impulse = Vector2.zero; //killing impulse, be aware when implementing throw!
                NotifyPlayers();
                Debug.LogFormat($"{gameObject.name}: stopped floating @{serverTransformState.Position}");
            }
        }
    }

    private bool CanDriftTo(Vector3Int goal)
    {
        //FIXME: deOffset is a temp solution to this weird matrix 1,1 offset (but don't touch it unless you re-enable ObjectPool parenting)
        return matrix != null && matrix.IsEmptyAt(goal + deOffset);
    }
}