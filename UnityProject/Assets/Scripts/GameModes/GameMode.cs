using System;
using System.Collections.Generic;
using UnityEngine;
using Antagonists;
using Random = UnityEngine.Random;

/// <summary>
/// Contains the definition of a game mode. To create a new one you should
/// make a new class which inherits this one. Make a prefab with this script
/// attached so you can define the values in the editor and define your
/// game mode functions in the child class.
/// </summary>
public abstract class GameMode : ScriptableObject
{
	#region Inspector Values

	[Header("General Settings")]
	/// <summary>
	/// The name of the game mode
	/// </summary>
	[Tooltip("The name of the game mode")]
	[SerializeField]
	private string gameModeName;
	public string Name => gameModeName;

	/// <summary>
	/// The description of the game mode
	/// </summary>
	[Tooltip("A description of the game mode")]
	[SerializeField]
	[TextArea]
	private string description;
	public string Description => description;

	/// <summary>
	/// Is respawning enabled in this game mode
	/// </summary>
	[Tooltip("Should players be allowed to respawn?")]
	[SerializeField]
	private bool canRespawn;
	public bool CanRespawn => canRespawn;

	/// <summary>
	/// The minimum amount of players needed for the game mode to be possible. Can't be lower than 1.
	/// </summary>
	[Tooltip("What is the minimum amount of players needed to play this game mode?")]
	[SerializeField]
	[Min(1)]
	private int minPlayers = 1;
	public int MinPlayers => minPlayers;

	[Header("Antagonist Settings")]
	/// <summary>
	/// The ratio of antagonists to spawn for this game mode
	/// </summary>
	[Tooltip("Ratio of antagonists to player count. A value of 0.2 means there would be " +
			 "2 antagonists when there are 10 players.")]
	[SerializeField]
	[Range(0, 1)]
	private float antagRatio = 0;
	public float AntagRatio => antagRatio;

	/// <summary>
	/// The minimum amount of antags needed for the game mode to be possible.
	/// If <see cref="requiresMinAntags"/> is false, the number of chosen antags will be rounded up to this number.
	/// </summary>
	[Tooltip("The minimum amount of antags needed for the game mode to be possible. " +
			 "If requiresMinAntags is false, the number of chosen antags will be rounded up to this number.")]
	[SerializeField]
	[Min(0)]
	private int minAntags = 0;
	public int MinAntags => minAntags;

	/// <summary>
	/// Is the game mode possible if the <see cref="antagRatio"/> doesn't meet the <see cref="minAntags"/>?
	/// E.g. If true, when antagRatio is 0.2 and minAntags is 1, then you need at least 5 players to start the game mode.
	/// </summary>
	[Tooltip("Is the game mode possible if the player count multiplied by the antagRatio doesn't meet the minAntags? " +
			 "E.g. If true, when antagRatio is 0.2 and minAntags is 1, you need at least 5 players to start the game mode.")]
	[SerializeField]
	private bool requiresMinAntags;
	public bool RequiresMinAntags => requiresMinAntags;

	/// <summary>
	/// Are antags on the same team or are they lone wolves?
	/// Used for the end of round antag report.
	/// </summary>
	[Tooltip("Are antags on the same team or are they lone wolves?" +
			 "Used for the end of round antag report.")]
	[SerializeField]
	private bool teamGameMode;
	public bool TeamGameMode => teamGameMode;

	/// <summary>
	/// Can antags spawn during the round?
	/// </summary>
	[Tooltip("Can antags spawn during the round?")]
	[SerializeField]
	private bool midRoundAntags;
	public bool MidRoundAntags => midRoundAntags;

	/// <summary>
	/// The possible antagonists for this game mode
	/// </summary>
	[Tooltip("The possible antagonists for this game mode")]
	[SerializeField]
	private List<Antagonist> possibleAntags;
	public List<Antagonist> PossibleAntags => possibleAntags;

	/// <summary>
	/// The JobTypes that cannot be chosen as antagonists for this game mode
	/// </summary>
	[Tooltip("The JobTypes that cannot be chosen as antagonists for this game mode")]
	[SerializeField]
	private List<JobType> nonAntagJobTypes = new List<JobType>
	{
		JobType.CAPTAIN,
		JobType.HOP,
		JobType.HOS,
		JobType.WARDEN,
		JobType.SECURITY_OFFICER,
		JobType.DETECTIVE,
	};
	public List<JobType> NonAntagJobTypes => nonAntagJobTypes;

	#endregion

	#region Game Mode Methods

	/// <summary>
	/// Check if the game mode meets the minimum player and antag requirements.
	/// Override this to add other checks for your game mode.
	/// </summary>
	public virtual bool IsPossible()
	{
		int players = PlayerList.Instance.ConnectionCount;
		return players >= MinPlayers && (!RequiresMinAntags ||
										 (Math.Floor(players * antagRatio) >= MinAntags));
	}

	/// <summary>
	/// Set up anything needed for the game mode before the RoundStarted event is triggered.
	/// Override this if you need any custom logic.
	/// </summary>
	public virtual void SetupRound()
	{
		Logger.LogFormat("Setting up {0} round!", Category.GameMode, Name);
	}

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
	protected virtual bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
	{
		// Don't spawn any mid round antags if game mode doesn't allow it
		if (!MidRoundAntags)
		{
			return false;
		}

		// Populates antags based on the non-antag job types and ratios
		int players = PlayerList.Instance.InGamePlayers.Count;
		return !NonAntagJobTypes.Contains(spawnRequest.RequestedOccupation.JobType) &&
			   AntagManager.Instance.AntagCount < Math.Floor(players * AntagRatio) &&
			   players > 0;
	}

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
	/// Start the round
	/// </summary>
	public virtual void StartRound()
	{
		Logger.LogFormat("Starting {0} round!", Category.GameMode, Name);

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
		Logger.LogFormat("Ending {0} round!", Category.GameMode, Name);
		AntagManager.Instance.ShowAntagStatusReport();
	}

	// /// <summary>
	// /// Override this to choose players to be antags
	// /// </summary>
	// public abstract void ChooseAntags();
	#endregion
}