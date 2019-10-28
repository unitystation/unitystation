
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
	Ended
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
	/// The state of the current round
	/// </summary>
	public RoundState CurrentRoundState;

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
	/// </summary>
	public string GetGameModeName()
	{
		return SecretGameMode ? "Secret" : GameMode.Name;
	}
}