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
	/// Checks if the conditions are met to spawn an antag, and spawns them
	/// as the antag if so, spawning them as an actual player and transferring them into the body
	/// (meaning there's no need to call PlayerSpawn.ServerSpawnPlayer). Does nothing
	/// if the conditions are not met to spawn this viewer as an antag.
	/// </summary>
	/// <param name="spawnRequest">spawn requested by the player</param>
	/// <returns>true if the viewer was spawned as an antag.</returns>
	public bool TrySpawnAntag(PlayerSpawnRequest spawnRequest)
	{
		if (ShouldSpawnAntag(spawnRequest))
		{
			SpawnAntag(spawnRequest);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Check if the joined viewer should be spawned as an antag (prior to actually
	/// spawning them).
	/// </summary>
	/// <param name="spawnRequest">player's spawn request, which should be used to determine
	/// if they should spawn as an antag</param>
	/// <returns>true if an antag should be spawned.</returns>
	protected abstract bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest);

	/// <summary>
	/// Spawn the player requesting the spawn as an antag, includes creating their player object
	/// and transferring them to it. This is used as an alternative
	/// to PlayerSpawn.ServerSpawnPlayer when an antag should be spawned.
	///
	/// Defaults to picking a random antag from the possible antags list and
	/// spawning them as per the antag-specific spawn logic.
	/// </summary>
	protected void SpawnAntag(PlayerSpawnRequest playerSpawnRequest)
	{
		if (PossibleAntags.Count > 0)
		{
			int randIndex = Random.Range(0, PossibleAntags.Count);
			AntagManager.Instance.ServerSpawnAntag(PossibleAntags[randIndex], playerSpawnRequest);
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
		// Allocate jobs to all ready players and spawn them
		var jobAllocator = new JobAllocator();
		var playerSpawnRequests = jobAllocator.DetermineJobs(PlayerList.Instance.ReadyPlayers);
		foreach (var spawnReq in playerSpawnRequests)
		{
			PlayerSpawn.ServerSpawnPlayer(spawnReq);
		}
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