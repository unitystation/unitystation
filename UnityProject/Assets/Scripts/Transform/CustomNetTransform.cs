using System.Collections;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

public struct TransformState
{
    public bool Active;
    public float Speed;
    public Vector3 Position;
}

//todo: check flow to avoid redundant messages
//todo: consider moving unregistering here
public class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
    public float Speed = 2; //lerp speed

    protected RegisterTile registerTile;

    private TransformState serverTransformState; //used for syncing with players
    private TransformState transformState;
    
    private Vector2 lastDirection;
    
    protected Matrix matrix;

    public override void OnStartServer()
    {
        InitServerState();
        base.OnStartServer();
    }

    private void InitServerState()
    {
        if ( isServer )
        {
            var position = Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
            serverTransformState = new TransformState {Active = true, Position = position, Speed = this.Speed};
            transformState = serverTransformState;
            NotifyPlayers();
        }
    }

    public override void OnStartClient()
    {
        StartCoroutine(WaitForLoad());
        base.OnStartClient();
    }

    IEnumerator WaitForLoad()
    {
//        yield return new WaitForEndOfFrame();
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
    
    [Server]
    public void DisappearFromWorldServer(/*bool forceUpdate = true*/)
    {
        //be careful with forceupdate=false, it should be false only to the initiator w/preditction (if at all)
        serverTransformState.Active = false;
        serverTransformState.Position = Vector3.zero;// = new TransformState {Active = false, Position = Vector3.zero};
        NotifyPlayers();
    }

    [Server]
    public void AppearAtPositionServer(Vector3 pos/*, bool forceUpdate = true*/)
    {
        serverTransformState.Active = true;
        SetPosition( pos );
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
        transformState = newState;
        transform.position = transformState.Position;
        if ( !transformState.Speed.Equals(0f) )
        {
            Speed = transformState.Speed;
        }
//        if ( gameObject.activeInHierarchy && transformState.Active )
//        {
//            Lerp();
//        }
        
        updateActiveStatus();
    }

    private void updateActiveStatus()
    {
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
        
        if ( transformState.Position != transform.position && transformState.Active )
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
        transform.position = Vector3.MoveTowards(transform.position, transformState.Position, Speed * Time.deltaTime);
    }

    /// <summary>
    /// trying to make drift detection serverside only, and then just send state updates to lerp to
    /// </summary>
    [Server]
    private void CheckSpaceDrift()
    {
        var pos = Vector3Int.RoundToInt(serverTransformState.Position);
        if( !pos.Equals(Vector3Int.zero) && matrix != null && matrix.IsFloatingAt(pos) )
        {
            var newGoal = Vector3Int.RoundToInt(transform.position + (Vector3) lastDirection);
            if ( matrix.IsFloatingAt(newGoal) )
            {
                SetPosition(newGoal);
            }
            else
            {
                Debug.LogFormat($"{gameObject.name}: not floating due to an obstacle at {newGoal}");
            }
        }
    }
}