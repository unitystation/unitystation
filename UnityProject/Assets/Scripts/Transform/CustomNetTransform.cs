using System.Collections;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

public struct TransformState
{
    public bool Active;
    public float Speed;
    public Vector2 Impulse;
    public Vector3 Position;
}

//todo: check flow and figure out double message for equipment; check if UpdateManager behaves normally
//todo: consider moving unregistering here
public class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
    public static readonly Vector3Int InvalidPos = new Vector3Int(0, 0, -100)
                                      , deOffset = new Vector3Int(-1, -1, 0);
    
    public float Speed = 2; //lerp speed

    protected RegisterTile registerTile;

    private TransformState serverTransformState; //used for syncing with players
    private TransformState transformState;
    
//    private Vector2 impulse = Vector2.left;

    
    protected Matrix matrix;

    public override void OnStartServer()
    {
        InitServerState();
        base.OnStartServer();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }



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
            transformState = serverTransformState;
//            NotifyPlayers(); this breaks message IDs!!!!!!!!!!!!
        }
    }

    public override void OnStartClient()
    {
        StartCoroutine(WaitForLoad());
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
    
    [Server]
    public void DisappearFromWorldServer(/*bool forceUpdate = true*/)
    {
        //be careful with forceupdate=false, it should be false only to the initiator w/preditction (if at all)
        serverTransformState.Active = false;
        serverTransformState.Position = InvalidPos;// = new TransformState {Active = false, Position = Vector3.zero};
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
        UpdateClientState(transformState);
    }

    /// <summary>
    /// Convenience method to make stuff appear at position
    /// For client prediction purposes.
    /// </summary>
    public void AppearAtPosition(Vector3 pos/*, bool forceUpdate = true*/)
    {
        transformState.Active = true;
        transformState.Position = pos;
        UpdateClientState(transformState);
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

        gameObject.SetActive(transformState.Active);
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
        
        CheckSpaceDrift();

        if ( GameData.IsHeadlessServer )
        {
            return;
        }
        
        if ( transformState.Position != transform.position )
        {
            Lerp();
        }

        //Registering
        if ( registerTile.Position != Vector3Int.RoundToInt(transformState.Position) )
        {
            RegisterObjects();
        }
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
            var newGoal = Vector3Int.RoundToInt(serverTransformState.Position + ( Vector3 ) serverTransformState.Impulse);
            //FIXME: deOffset is a temp solution to this weird matrix 1,1 offset
            if ( matrix.IsEmptyAt(newGoal + deOffset) ) 
            {    //Spess drifting
                serverTransformState.Position = newGoal;
                transformState.Position = newGoal;
            }
            else //Stopping drift
            {
                serverTransformState.Impulse = Vector2.zero; //killing impulse, be aware when doing throw!
                NotifyPlayers();
                Debug.LogFormat($"{gameObject.name}: stopped floating @{serverTransformState.Position}");
            }
        }
    }

    private bool hasImpulse()
    {
        return !serverTransformState.Impulse.Equals(Vector2.zero);
    }
}