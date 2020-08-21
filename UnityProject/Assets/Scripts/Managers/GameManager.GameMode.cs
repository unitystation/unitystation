
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents what state the round is in: PreRound, Started or Ended
/// </summary>
public enum RoundState
{
	None,
	PreRound,
	Started,
	Ended,
	Restarting,
}

/// <summary>
/// Game mode related part of GameManager
/// </summary>
public partial class GameManager
{
	[Header("Game Mode Fields")]
	/// <summary>
	/// Holds all gamemodes
	/// </summary>
	[SerializeField]
	private GameModeData GameModeData = null;

	/// <summary>
	/// Is the current game mode being kept secret?
	/// </summary>
	public bool SecretGameMode = true;

	/// <summary>
	/// Array of jobs from a randomized department. Used for Rebels gamemode (ex Cargonia)
	/// </summary>
	public List<JobType> Rebels;

	/// <summary>
	/// The state of the current round
	/// </summary>
	public RoundState CurrentRoundState
	{
		get => currentRoundState;
		private set
		{
			currentRoundState = value;
			Logger.LogFormat("CurrentRoundState is now {0}!", Category.Round, value);
		}
	}

	private RoundState currentRoundState;

	/// <summary>
	/// The current game mode
	/// </summary>
	private GameMode GameMode;

	/// <summary>
	/// Sets the current gamemode using a string to find the gamemode name
	/// </summary>
	public void SetGameMode(string gmName)
	{
		GameMode selectedGM = GameModeData.GetGameMode(gmName);
		SetGameMode(selectedGM);
	}

	public List<string> GetAvailableGameModeNames()
	{
		return GameModeData.GetAvailableGameModeNames();
	}

	/// <summary>
	/// Sets the current gamemode
	/// </summary>
	public void SetGameMode(GameMode gm)
	{
		Logger.Log($"Set game mode to: {gm.Name}", Category.GameMode);
		GameMode = gm;
	}

	/// <summary>
	/// Sets a random gamemode which is currently possible
	/// </summary>
	public void SetRandomGameMode()
	{
		// TODO add precondition checks
		GameMode randomGM = GameModeData.ChooseGameMode();
		SetGameMode(randomGM);
	}

	/// <summary>
	/// Gets the current game mode name. Will return Secret if it is being hidden.
	/// Override secret is for getting game mode name on the server
	/// </summary>
	public string GetGameModeName(bool overrideSecret = false)
	{
		if (overrideSecret)
		{
			if (GameMode == null)
			{
				return "null";
			}
			else
			{
				return GameMode.Name;
			}
		}

		return SecretGameMode ? "Secret" : GameMode.Name;
	}

	/// <summary>
	/// Checks if the conditions are met to spawn an antag, and spawns them
	/// as the antag if so, spawning them as an actual player and transferring them into the body
	/// (meaning there's no need to call PlayerSpawn.ServerSpawnPlayer). Does nothing
	/// if the conditions are not met to spawn this viewer as an antag.
	///
	/// </summary>
	/// <param name="spawnRequest">spawn requested by the player</param>
	/// <returns>true if the player was spawned as an antag.</returns>
	public bool TrySpawnAntag(PlayerSpawnRequest spawnRequest)
	{
		return GameMode.TrySpawnAntag(spawnRequest);
	}

	/// <summary>
	/// Waits before starting the game mode (to stop players being spawned in before everything has initialised)
	/// </summary>
	private IEnumerator WaitToStartGameMode()
	{
		yield return WaitFor.EndOfFrame;

		foreach (var job in GameMode.PossibleAntags)
		{
			if (job.AntagOccupation != null && job.AntagOccupation.JobType == JobType.SYNDICATE)
			{
				yield return StartCoroutine(SubSceneManager.Instance.LoadSyndicate());
				break;
			}
		}

		GameMode.StartRound();
	}
}