using UnityEngine;
using Mirror;

/// Server-only full player information class
public class ConnectedPlayer
{
	private string username;
	private string name;
	private JobType job;
	private ulong steamId;
	private GameObject gameObject;
	private PlayerScript playerScript;
	private JoinedViewer viewerScript;
	private NetworkConnection connection;
	private string clientID;

	/// Flags if player received a bunch of sync messages upon joining
	private bool synced;

	//Name that is used if the client's character name is empty
	private const string DEFAULT_NAME = "Anonymous Spessman";

	public bool IsAuthenticated => steamId != 0;

	public static readonly ConnectedPlayer Invalid = new ConnectedPlayer
	{
		connection = new NetworkConnection("0.0.0.0"),
		gameObject = null,
		username = null,
		name = "kek",
		job = JobType.NULL,
		steamId = 0,
		synced = true,
		clientID = ""
	};

	public static ConnectedPlayer ArchivedPlayer( ConnectedPlayer player )
	{
		return new ConnectedPlayer
		{
			connection = Invalid.Connection,
			gameObject = player.GameObject,
			username = player.Username,
			name = player.Name,
			job = player.Job,
			steamId = player.SteamId,
			synced = player.synced,
			clientID = player.clientID
		};
	}

	public PlayerScript Script => playerScript;
	public JoinedViewer ViewerScript => viewerScript;

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
			playerScript = value.GetComponent<PlayerScript>();
			viewerScript = value.GetComponent<JoinedViewer>();
		}
	}

	public string Username
	{
		get => username;
		set => username = value;
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
				Logger.Log( $"Updated steamID! {this}" , Category.Steam);
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

	public string ClientId
	{
		get => clientID;
		set => clientID = value;
	}

	public bool HasNoName()
	{
		return name == null || name.Trim().Equals("") || name == DEFAULT_NAME;
	}

	/// <summary>
	/// Checks against a set of rules for user names like Null or whitespace
	/// </summary>
	/// <param name="newName"></param>
	/// <returns>True if the name passes all tests, false if it does not</returns>
	public static bool isValidName(string newName)
	{
		if (string.IsNullOrWhiteSpace(newName))
		{
			return false;
		}else{
			return true;
		}
	}

	private void TryChangeName(string playerName)
	{
		//When a ConnectedPlayer object is initialised it has a null value
		//We want to make sure that it gets set to something if the client requested something bad
		//Issue #1377
		if (isValidName(playerName) == false)
		{
			Logger.LogWarningFormat("Attempting to assign invalid name to ConnectedPlayer. Assigning default name ({0}) instead", Category.Server, DEFAULT_NAME);
			playerName = DEFAULT_NAME;
		}

		//Player name is unchanged, return early.
		if(playerName == name)
		{
			return;
		}

		var playerList = PlayerList.Instance;
		if ( playerList == null )
		{
//			Logger.Log("PlayerList not instantiated, setting name blindly");
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
			Logger.LogTrace($"TRYING: {proposedName}", Category.Connections);
		}
		if ( PlayerList.Instance.ContainsName(proposedName) )
		{
			Logger.LogTrace($"NAME ALREADY EXISTS: {proposedName}", Category.Connections);
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
