using Messages.Server;
using UnityEngine;
using Mirror;

/// <summary>
/// Server-only full player information class
/// </summary>
public class ConnectedPlayer
{
	/// <summary>
	/// Name that is used if the client's character name is empty
	/// </summary>
	private const string DEFAULT_NAME = "Anonymous Spessman";
	public static readonly ConnectedPlayer Invalid = new ConnectedPlayer
	{
		Connection = null,
		gameObject = null,
		Username = null,
		name = "kek",
		job = JobType.NULL,
		ClientId = "",
		UserId = ""
	};

	public string Username { get; set; }
	private string name;
	private JobType job;
	private GameObject gameObject;
	public PlayerScript Script { get; private set; }
	public JoinedViewer ViewerScript { get; private set; }
	public string ClientId { get; set; }
	public string UserId { get; set; }
	public NetworkConnection Connection { get; set; }
	public CharacterSettings CharacterSettings { get; set; }

	public GameObject GameObject
	{
		get => gameObject;
		set
		{
			gameObject = value;
			if (Script)
			{
				Script.connectedPlayer = null;
			}
			if (gameObject != null)
			{
				// If player is in lobby, their controlled GameObject is JoinedViewer (which has JoinedViewer component).
				// Else they're in the game and so have a GameObject that has PlayerScript attached.
				Script = value.GetComponent<PlayerScript>();
				if (Script)
				{
					Script.connectedPlayer = this;
				}
				ViewerScript = value.GetComponent<JoinedViewer>();
			}
			else
			{
				Script = null;
				ViewerScript = null;
			}
		}
	}

	/// <summary>
	/// The in-game name of the player. Does not take into account recognition (unknown identity).
	/// </summary>
	public string Name
	{
		get
		{
			if (string.IsNullOrEmpty(name))
			{
				return gameObject.name;
			}
			return name;
		}
		set
		{
			TryChangeName(value);
			TrySendUpdate();
		}
	}

	public JobType Job
	{
		get => job;
		set
		{
			job = value;
			TrySendUpdate();
		}
	}

	private void TryChangeName(string playerName)
	{
		//When a ConnectedPlayer object is initialised it has a null value
		//We want to make sure that it gets set to something if the client requested something bad
		//Issue #1377
		if (string.IsNullOrWhiteSpace(playerName))
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
			name = playerName;
			return;
		}

		string uniqueName = GetUniqueName(playerName, UserId);
		name = uniqueName;
	}

	/// <summary>
	/// Generating a unique name (Player -> Player2 -> Player3 ...)
	/// </summary>
	/// <param name="name"></param>
	/// <param name="sameNames"></param>
	/// <returns></returns>
	private static string GetUniqueName(string name, string _UserId ,int sameNames = 0)
	{
		while (true)
		{
			string proposedName = name;
			if (sameNames != 0)
			{
				proposedName = $"{name}{sameNames + 1}";
				Logger.LogTrace($"TRYING: {proposedName}", Category.Connections);
			}

			if (!PlayerList.Instance.ContainsName(proposedName,_UserId))
			{
				return proposedName;
			}

			Logger.LogTrace($"NAME ALREADY EXISTS: {proposedName}", Category.Connections);
			sameNames++;
		}
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
		if (this == Invalid)
		{
			return "Invalid player";
		}
		return $"ConnectedPlayer {nameof(Username)}: {Username}, {nameof(ClientId)}: {ClientId}, {nameof(UserId)}: {UserId}, {nameof(Connection)}: {Connection}, {nameof(Name)}: {Name}, {nameof(Job)}: {Job}";
	}
}
