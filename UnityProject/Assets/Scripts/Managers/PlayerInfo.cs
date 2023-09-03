using System;
using Logs;
using UnityEngine;
using Mirror;
using Player;
using Messages.Server;
using Systems.Character;

/// <summary>
/// Server-only full player information class
/// </summary>
public class PlayerInfo
{
	/// <summary>
	/// Name that is used if the client's character name is empty
	/// </summary>
	private const string DEFAULT_NAME = "Anonymous Spessman";
	public static readonly PlayerInfo Invalid = new PlayerInfo
	{
		Connection = null,
		gameObject = null,
		Username = null,
		name = "Invalid Player",
		job = JobType.NULL,
		ClientId = "",
		UserId = "",
		ConnectionIP = ""
	};

	/// <summary>Username for the player's account.</summary>
	public string Username { get; set; }

	/// <summary>The player script for the player while in the game.</summary>
	public PlayerScript Script => GameObject.OrNull()?.GetComponent<PlayerScript>();
	/// <summary>The player script for the player while in the lobby.</summary>
	public JoinedViewer ViewerScript { get;  set; }

	public Mind Mind { get; private set; }

	public string ClientId { get; set; }
	public string UserId { get; set; }
	public NetworkConnectionToClient Connection { get; set; }

	public string ConnectionIP { get; set; }

	public bool IsOnline { get; private set; }
	public PlayerRole PlayerRoles { get; set; }

	public bool IsAdmin => (PlayerRoles & PlayerRole.Admin) != 0;

	public bool IsOOCMuted = false;

	//This is only set when the player presses the ready button? But not if late joining, wtf?????
	public CharacterSheet RequestedCharacterSettings { get; set; }

	/// <summary>The player GameObject. Different GameObject if in lobby vs. in game.</summary>
	public GameObject GameObject //TODO Better system
	{
		get
		{
			if (Mind == null)
			{
				return gameObject;
			}
			else
			{
				return Mind.GetDeepestBody().gameObject;
			}

		}
		set
		{
			gameObject = value;
			if (Script)
			{
				Script.PlayerInfo = null;
			}

			if (gameObject != null)
			{
				// If player is in lobby, their controlled GameObject is JoinedViewer (which has JoinedViewer component).
				// Else they're in the game and so have a GameObject that has PlayerScript attached.
				if (Script)
				{
					Script.PlayerInfo = this;
				}
				ViewerScript = value.GetComponent<JoinedViewer>();
			}
			else
			{
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
				if (Mind != null)
				{
					return Mind.name;
				}

				if (RequestedCharacterSettings != null)
				{
					return RequestedCharacterSettings.Name;
				}

				if (gameObject != null)
				{
					return gameObject.name;
				}

				return "OH you shouldn't have this name!";

			}
			return name;
		}
		set
		{
			TryChangeName(value);
			TrySendUpdate();
		}
	}

	public JobType Job => Mind.OrNull()?.occupation.OrNull()?.JobType == null ? JobType.NULL : Mind.occupation.JobType;

	private string name;
	private JobType job;
	private GameObject gameObject;

	private void TryChangeName(string playerName)
	{
		//When a ConnectedPlayer object is initialised it has a null value
		//We want to make sure that it gets set to something if the client requested something bad
		//Issue #1377
		if (string.IsNullOrWhiteSpace(playerName))
		{
			Loggy.LogWarningFormat("Attempting to assign invalid name to ConnectedPlayer. Assigning default name ({0}) instead", Category.Server, DEFAULT_NAME);
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

	public void SetMind(Mind inMind)
	{
		if (Mind != null)
		{
			Mind.ControlledBy = null;
		}


		Mind = inMind;
		if (inMind != null)
		{
			Name = inMind.CurrentCharacterSettings.Name;
			inMind.ControlledBy = this;
		}
	}

	/// <summary>
	/// Generating a unique name (Player -> Player2 -> Player3 ...)
	/// </summary>
	/// <param name="name"></param>
	/// <param name="userId"></param>
	/// <param name="sameNames"></param>
	/// <returns></returns>
	private static string GetUniqueName(string name, string userId, int sameNames = 0)
	{
		while (true)
		{
			string proposedName = name;
			if (sameNames != 0)
			{
				proposedName = $"{name}{sameNames + 1}";
				Loggy.LogTrace($"TRYING: {proposedName}", Category.Connections);
			}

			if (!PlayerList.Instance.Has(proposedName, userId))
			{
				return proposedName;
			}

			Loggy.LogTrace($"NAME ALREADY EXISTS: {proposedName}", Category.Connections);
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
		return $"ConnectedPlayer {nameof(Username)}: {Username}, {nameof(ClientId)}: {ClientId}, " +
		       $"{nameof(UserId)}: {UserId}, {nameof(Connection)}: {Connection}, {nameof(Name)}: {Name}, {nameof(Job)}: {Job}";
	}
}

[Flags]
public enum PlayerRole
{
	Player = 0,
	Admin = 1,
	Mentor = 2,
}
