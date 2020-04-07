using UnityEngine;
using Mirror;

/// Server-only full player information class
public class ConnectedPlayer
{
	private string username;
	private string name;
	private JobType job;
	private GameObject gameObject;
	private PlayerScript playerScript;
	private JoinedViewer viewerScript;
	private NetworkConnection connection;
	private string clientID;
	private string userID;

	/// Flags if player received a bunch of sync messages upon joining
	private bool synced;

	//Name that is used if the client's character name is empty
	private const string DEFAULT_NAME = "Anonymous Spessman";

	public static readonly ConnectedPlayer Invalid = new ConnectedPlayer
	{
		connection = null,
		gameObject = null,
		username = null,
		name = "kek",
		job = JobType.NULL,
		synced = true,
		clientID = "",
		userID = ""
	};

	public static ConnectedPlayer ArchivedPlayer( ConnectedPlayer player )
	{
		return new ConnectedPlayer
		{
			connection = null,
			gameObject = player.GameObject,
			username = player.Username,
			name = player.Name,
			job = player.Job,
			synced = player.synced,
			clientID = player.clientID,
			userID = player.userID,
			viewerScript = player.ViewerScript
		};
	}

	public PlayerScript Script => playerScript;
	public JoinedViewer ViewerScript => viewerScript;

	public NetworkConnection Connection
	{
		get { return connection; }
		set { connection = value; }
	}

	public GameObject GameObject
	{
		get { return gameObject; }
		set
		{
			gameObject = value;
			if (gameObject != null)
			{
				playerScript = value.GetComponent<PlayerScript>();
				viewerScript = value.GetComponent<JoinedViewer>();
			}
			else
			{
				playerScript = null;
				viewerScript = null;
			}
		}
	}

	public string Username
	{
		get { return username; }
		set { username = value; }
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
		get { return clientID; }
		set { clientID = value; }
	}

	public string UserId
	{
		get { return userID; }
		set { userID = value; }
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
		if ( CustomNetworkManager.Instance != null
		     && CustomNetworkManager.Instance._isServer
		     && PlayerList.Instance != null )
		{
			UpdateConnectedPlayersMessage.Send();
		}
	}

	public override string ToString()
	{
		return $"[clientID={ClientId}|conn={Connection.connectionId}|go={gameObject}|name='{name}'|job={job}|synced={synced}]";
	}
}
