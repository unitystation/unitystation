using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Antagonists;
using UnityEngine.Serialization;
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
	[Tooltip("The name of the game mode")]
	[SerializeField]
	private string gameModeName;
	/// <summary>
	/// The name of the game mode
	/// </summary>
	public string Name => gameModeName;

	[Tooltip("A description of the game mode")]
	[SerializeField]
	[TextArea]
	private string description;
	/// <summary>
	/// The description of the game mode
	/// </summary>
	public string Description => description;

	[Tooltip("Should players be allowed to respawn?")]
	[SerializeField]
	private bool canRespawn;
	/// <summary>
	/// Is respawning enabled in this game mode
	/// </summary>
	public bool CanRespawn => canRespawn;

	[Tooltip("What is the minimum amount of players needed to play this game mode?")]
	[SerializeField]
	[Min(1)]
	private int minPlayers = 1;
	/// <summary>
	/// The minimum amount of players needed for the game mode to be possible. Can't be lower than 1.
	/// </summary>
	public int MinPlayers => minPlayers;

	[Header("Antagonist Settings")]
	[Tooltip("Ratio of antagonists to player count. A value of 0.2 means there would be " +
			 "2 antagonists when there are 10 players.")]
	[SerializeField]
	[Range(0, 1)]
	private float antagRatio = 0;
	/// <summary>
	/// The ratio of antagonists to spawn for this game mode
	/// </summary>
	public float AntagRatio => antagRatio;

	[Tooltip("The minimum amount of antags needed for the game mode to be possible. " +
			 "If requiresMinAntags is false, the number of chosen antags will be rounded up to this number.")]
	[SerializeField]
	[Min(0)]
	private int minAntags = 0;
	/// <summary>
	/// The minimum amount of antags needed for the game mode to be possible.
	/// If <see cref="requiresMinAntags"/> is false, the number of chosen antags will be rounded up to this number.
	/// </summary>
	public int MinAntags => minAntags;

	[Tooltip("Is the game mode possible if the player count multiplied by the antagRatio doesn't meet the minAntags? " +
			 "E.g. If true, when antagRatio is 0.2 and minAntags is 1, you need at least 5 players to start the game mode.")]
	[SerializeField]
	private bool requiresMinAntags;
	/// <summary>
	/// Is the game mode possible if the <see cref="antagRatio"/> doesn't meet the <see cref="minAntags"/>?
	/// E.g. If true, when antagRatio is 0.2 and minAntags is 1, then you need at least 5 players to start the game mode.
	/// </summary>
	public bool RequiresMinAntags => requiresMinAntags;

	[Tooltip("Are antags on the same team or are they lone wolves?" +
			 "Used for the end of round antag report.")]
	[SerializeField]
	private bool teamGameMode;
	/// <summary>
	/// Are antags on the same team or are they lone wolves?
	/// Used for the end of round antag report.
	/// </summary>
	public bool TeamGameMode => teamGameMode;

	[Tooltip("Can antags spawn during the round?")]
	[SerializeField]
	private bool midRoundAntags;
	/// <summary>
	/// Can antags spawn during the round?
	/// </summary>
	public bool MidRoundAntags => midRoundAntags;

	[FormerlySerializedAs("chooseAntagsBeforeJobs")]
	[Tooltip("Should antags be allocated a job? If true, will choose antags after allocating jobs.")]
	[SerializeField]
	private bool allocateJobsToAntags;
	/// <summary>
	/// Should antags be allocated a job?
	/// If true, will choose antags after allocating jobs.
	/// </summary>
	public bool AllocateJobsToAntags => allocateJobsToAntags;

	[Tooltip("The possible antagonists for this game mode")]
	[SerializeField]
	private List<Antagonist> possibleAntags;
	/// <summary>
	/// The possible antagonists for this game mode
	/// </summary>
	public List<Antagonist> PossibleAntags => possibleAntags;

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
	/// <summary>
	/// The JobTypes that cannot be chosen as antagonists for this game mode
	/// </summary>
	public List<JobType> NonAntagJobTypes => nonAntagJobTypes;

	#endregion

	#region Game Mode Methods

	/// <summary>
	/// Check if the game mode meets the minimum player and antag requirements.
	/// Override this to add other checks for your game mode.
	/// </summary>
	public virtual bool IsPossible()
	{
		int players = PlayerList.Instance.ReadyPlayers.Count;
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
	/// Only called for mid-round joiners.
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
	/// Only called for mid-round joiners.
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

		// Determine if antag based on the non-antag job types and ratios
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
		if (PossibleAntags.Count <= 0)
		{
			Logger.LogError("PossibleAntags is empty! Game mode must have some if spawning antags.",
				Category.GameMode);
			return;
		}

		int randIndex = Random.Range(0, PossibleAntags.Count);
		var antag = PossibleAntags[randIndex];
		if (!AllocateJobsToAntags &&antag.AntagOccupation == null)
		{
			Logger.LogErrorFormat("AllocateJobsToAntags is false but {0} AntagOccupation is null! " +
								  "Game mode must either set AllocateJobsToAntags or possible antags neeed an AntagOccupation.",
				Category.GameMode, antag.AntagName);
			return;
		}
		AntagManager.Instance.ServerSpawnAntag(PossibleAntags[randIndex], playerSpawnRequest);
	}

	/// <summary>
	/// Start the round
	/// </summary>
	public virtual void StartRound()
	{
		Logger.LogFormat("Starting {0} round!", Category.GameMode, Name);

		List<PlayerSpawnRequest> playerSpawnRequests;
		List<PlayerSpawnRequest> antagSpawnRequests;
		int antagsToSpawn = CalculateAntagCount(PlayerList.Instance.ReadyPlayers.Count);
		var jobAllocator = new JobAllocator();
		var playerPool = PlayerList.Instance.ReadyPlayers;
		if (AllocateJobsToAntags)
		{
			// Allocate jobs to all players first then choose antags
			playerSpawnRequests = jobAllocator.DetermineJobs(playerPool);
			// TODO: add antag pref checks here
			var antagCandidates = playerSpawnRequests.Where(p =>
					!NonAntagJobTypes.Contains(p.RequestedOccupation.JobType));
			antagSpawnRequests = antagCandidates.PickRandom(antagsToSpawn).ToList();
			// Player and antag spawn requests are kept separate to stop players being spawned twice
			playerSpawnRequests.RemoveAll(antagSpawnRequests.Contains);
		}
		else
		{
			// Choose antags first then allocate jobs to all other players
			// TODO: add antag pref checks here
			var antagCandidates = playerPool;
			var chosenAntags = antagCandidates.PickRandom(antagsToSpawn).ToList();
			// Player and antag spawn requests are kept separate to stop players being spawned twice
			playerPool.RemoveAll(chosenAntags.Contains);
			playerSpawnRequests = jobAllocator.DetermineJobs(playerPool);
			antagSpawnRequests = chosenAntags.Select(player =>
				PlayerSpawnRequest.RequestOccupation(player, null)).ToList();
		}

		// Spawn all players and antags
		foreach (var spawnReq in playerSpawnRequests)
		{
			PlayerSpawn.ServerSpawnPlayer(spawnReq);
		}
		foreach (var spawnReq in antagSpawnRequests)
		{
			SpawnAntag(spawnReq);
		}
	}

	/// <summary>
	/// Calculates how many antags should be chosen at round start based on the player count.
	/// </summary>
	private int CalculateAntagCount(int playerCount)
	{
		var antagCount = (int)Math.Floor(playerCount * antagRatio);
		// If RequiresMinAntags is true then round up to MinAntags if antagCount is below
		return RequiresMinAntags ? Math.Max(MinAntags, antagCount) : antagCount;
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

	#endregion
}