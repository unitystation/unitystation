
using System.Collections;
using System.Collections.Generic;
using GameModes;
using Logs;
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
		set
		{
			currentRoundState = value;
			Loggy.LogFormat("CurrentRoundState is now {0}!", Category.Round, value);
		}
	}

	private RoundState currentRoundState;

	/// <summary>
	/// The current game mode
	/// </summary>
	public GameMode GameMode { get; private set; }

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
		Loggy.Log($"Set game mode to: {gm.Name}", Category.GameMode);
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
		if (SecretGameMode && overrideSecret == false)
		{
			return "Secret";
		}

		if (GameMode == null)
		{
			return "null";
		}

		return GameMode.Name;
	}

	/// <summary>
	/// Waits before starting the game mode (to stop players being spawned in before everything has initialised)
	/// </summary>
	private IEnumerator WaitToStartGameMode()
	{
		yield return WaitFor.EndOfFrame;

		foreach (var job in GameMode.PossibleAntags)
		{
			if (job.AntagOccupation == null) continue;

			// We wait an extra frame after loading each additional scene so that MatrixInfo is ready for player spawning.
			// If MatrixInfo is not ready, players that spawn in the additional scenes (wizard ship, syndicate base)
			// will spawn on the wrong matrix and so will exhibit space exposure symptoms.

			if (job.AntagOccupation.JobType == JobType.SYNDICATE)
			{
				yield return StartCoroutine(SubSceneManager.Instance.LoadSyndicate());
				yield return WaitFor.EndOfFrame;
				break;
			}

			if (job.AntagOccupation.JobType == JobType.WIZARD)
			{
				yield return StartCoroutine(SubSceneManager.Instance.LoadWizard());
				yield return WaitFor.EndOfFrame;
				break;
			}
		}

		GameMode.StartRound();
	}
}
