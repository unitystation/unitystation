using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Antagonists;

/// <summary>
/// Contains the definition of a game mode. To create a new one you should
/// make a new class which inherits this one. Make a prefab with this script
/// attached so you can define the values in the editor and define your
/// game mode functions in the child class.
/// </summary>
public abstract class GameMode : ScriptableObject
{
	/// <summary>
	/// The name of the game mode
	/// </summary>
	[Tooltip("The name of the game mode")]
	public string Name;

	/// <summary>
	/// The description of the game mode
	/// </summary>
	[Tooltip("A description of the game mode")]
	public string Description;

	/// <summary>
	/// Is respawning enabled in this game mode
	/// </summary>
	[Tooltip("Should players be allowed to respawn?")]
	public bool CanRespawn;

	/// <summary>
	/// The minimum amount of players needed for the game mode
	/// </summary>
	[Tooltip("What is the minimum amount of players needed for this game mode?")]
	public int MinPlayers;

	/// <summary>
	/// The minimum amount of antags needed in the game mode
	/// </summary>
	[Tooltip("What is the minimum amount of antagonists needed for this game mode?")]
	public int MinAntags;

	/// <summary>
	/// The possible antagonists for this game mode
	/// </summary>
	public List<Antagonist> PossibleAntags;

	// ================= Game Mode Methods =================

	/// <summary>
	/// Check if the game mode meets the minimum player requirements
	/// </summary>
	public virtual bool IsPossible()
	{
		//TODO add more checks in future
		return PlayerList.Instance.ConnectionCount >= MinPlayers;
	}

	/// <summary>
	/// Set up everything for the game mode
	/// </summary>
	public abstract void SetupRound();

	/// <summary>
	/// Check if more antags are needed. Should be defined by each game mode.
	/// </summary>
	public abstract void CheckAntags();

	/// <summary>
	/// Defines how to spawn an antag for this game mode.
	/// Defaults to picking a random antag from the possible antags list.
	/// </summary>
	public virtual void SpawnAntag(ConnectedPlayer player = null)
	{
		if (PossibleAntags.Count > 0)
		{
		int randIndex = Random.Range(0, PossibleAntags.Count);
		Antagonist gmAntag = Instantiate(PossibleAntags[randIndex]);
		AntagManager.Instance.CreateAntag(gmAntag, player);
		}
	}

	/// <summary>
	/// Determine what to do with new players that join
	/// </summary>
	// public virtual void SetupNewPlayer()
	// {
	// 	if (GameManager.Instance.RoundStarted)
	// 	{
	// 		UIManager.Display.SetScreenForJobSelect();
	// 	}
	// }

	/// <summary>
	/// Start the round
	/// </summary>
	public virtual void StartRound()
	{
		// TODO remove once random job allocation is done
		UpdateUIMessage.Send(ControlDisplays.Screens.JobSelect);
	}

	/// <summary>
	/// Check if the round should end yet
	/// </summary>
	public virtual void CheckEndCondition()
	{
		Logger.Log("Checking end round conditions!", Category.GameMode);
	}

	/// <summary>
	/// End the round and display any relevant reports
	/// </summary>
	public virtual void EndRound()
	{
		Logger.Log("Ending round!", Category.GameMode);
		AntagManager.Instance.ShowAntagStatusReport();
	}

	// TODO
	// public abstract void ChooseAntags();
}