using UnityEngine;
using UnityEngine.Networking;

/// Server-only full player information class
public class ConnectedPlayer
{
    private string name;
    private JobType job;
    private ulong steamId;
    private GameObject gameObject;
    private NetworkConnection connection;
    /// Flags if player received a bunch of sync messages upon joining
    private bool synced;

    public bool IsAuthenticated => steamId != 0;

    public static readonly ConnectedPlayer Invalid = new ConnectedPlayer
    {
        connection = new NetworkConnection(),
        gameObject = null,
        name = "kek",
        job = JobType.NULL,
        steamId = 0,
        synced = true
    };

    public static ConnectedPlayer ArchivedPlayer( ConnectedPlayer player )
    {
        return new ConnectedPlayer
        {
            connection = Invalid.Connection,
            gameObject = player.GameObject,
            name = player.Name,
            job = player.Job,
            steamId = player.SteamId,
            synced = player.synced
        };
    }

    public NetworkConnection Connection
    {
        get { return connection; }
        set { connection = value ?? Invalid.Connection; }
    }

    public GameObject GameObject
    {
        get { return gameObject; }
        set
        {
            if ( PlayerList.Instance != null && gameObject )
            {
                //Add to history if player had different body previously
                PlayerList.Instance.AddPrevious( this );
            }
            gameObject = value;
        }
    }

    public string Name
    {
        get { return name; }
        set
        {
            TryChangeName(value);
            TrySendUpdate();
        }
    }

    public ulong SteamId
    {
        get { return steamId; }
        set
        {
            if ( value != 0 )
            {
                steamId = value;
                Debug.Log( $"Updated steamID! {this}" );
            }
        }
    }

    public JobType Job
    {
        get { return job; }
        set
        {
            job = value;
            TrySendUpdate();
        }
    }

    public bool Synced {
        get { return synced; }
        set { synced = value; }
    }

    public bool HasNoName()
    {
        return name == null || name.Trim().Equals("");
    }

    private void TryChangeName(string playerName)
    {
        if ( playerName == null || playerName.Trim().Equals("") || name == playerName )
        {
            return;
        }
        var playerList = PlayerList.Instance;
        if ( playerList == null )
        {
//			Debug.Log("PlayerList not instantiated, setting name blindly");
            name = playerName;
            return;
        }

        string uniqueName = GetUniqueName(playerName);
        name = uniqueName;
		
//        if ( !playerList.playerScores.ContainsKey(uniqueName) )
//        {
//            playerList.playerScores.Add(uniqueName, 0);
//        }
    }

    /// Generating a unique name (Player -> Player2 -> Player3 ...)
    private static string GetUniqueName(string name, int sameNames = 0)
    {
        string proposedName = name;
        if ( sameNames != 0 )
        {
            proposedName = $"{name}{sameNames + 1}";
            Debug.Log($"TRYING: {proposedName}");
        }
        if ( PlayerList.Instance.ContainsName(proposedName) )
        {
            Debug.Log($"NAME ALREADY EXISTS: {proposedName}");
            sameNames++;
            return GetUniqueName(name, sameNames);
        }

        return proposedName;
    }

    private static void TrySendUpdate()
    {
        if ( CustomNetworkManager.Instance != null && CustomNetworkManager.Instance._isServer && PlayerList.Instance != null )
        {
            UpdateConnectedPlayersMessage.Send();
        }
    }

    public override string ToString()
    {
        return $"[conn={Connection.connectionId}|go={gameObject}|name='{name}'|job={job}|steamId={steamId}|synced={synced}]";
    }
}