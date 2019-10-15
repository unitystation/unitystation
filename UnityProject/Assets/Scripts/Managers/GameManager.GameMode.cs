
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Represents what state the round is in: PreRound, Started or Ended
/// </summary>
public enum RoundState
{
	None,
	PreRound,
	Started,
	Ended
}

/// <summary>
/// Game mode related part of GameManager
/// </summary>
public partial class GameManager
{
	public List<GameMode> AllGameModes = new List<GameMode>();

	/// <summary>
	/// Is the current game mode being kept secret?
	/// </summary>
	public bool SecretGameMode = true;

	/// <summary>
	/// The state of the current round
	/// </summary>
	public RoundState CurrentRoundState;

	/// <summary>
	/// The current game mode
	/// </summary>
	private GameMode GameMode;

	/// <summary>
	/// Find all gamemode prefabs and add them to AllGameModes for selection
	/// </summary>
	private void RefreshAllGameModes()
	{
		// Only do this stuff on the server
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		AllGameModes.Clear();
		Logger.Log("Finding available gamemodes...", Category.GameMode);
		var prefabs = Resources.LoadAll("Prefabs/GameModes", typeof(GameObject));
		foreach(Object obj in prefabs)
		{
			var gm = ((GameObject) obj).GetComponent<GameMode>();
			Logger.Log($"Found GM: {gm.Name}", Category.GameMode);
			AllGameModes.Add(gm);
		}
	}

	public void SelectGameMode(string gmName)
	{
		// TODO add precondition checks
		foreach(GameMode gm in AllGameModes)
		{
			if (gm.Name == gmName)
			{
				Logger.Log($"Set game mode to: {gmName}", Category.GameMode);
				GameMode = gm;
			}
		}
	}

	/// <summary>
	/// Gets the current game mode name. Will return Secret if it is being hidden.
	/// </summary>
	public string GetGameModeName()
	{
		return SecretGameMode ? "Secret" : GameMode.Name;
	}

	/// <summary>
	/// Finds all possible game modes and randomly chooses one that is possible with the current number of players
	/// </summary>
	private void ChooseGameMode()
	{
		List<GameMode> possibleGMs = new List<GameMode>();
		foreach (var gm in AllGameModes)
		{
			if (gm.IsPossible())
			{
				possibleGMs.Add(gm);
			}
		}
		int index = Random.Range(0, possibleGMs.Count);
		GameMode = possibleGMs[index];
		Logger.Log($"Selected game mode: {GameMode.Name}", Category.GameMode);
	}
}