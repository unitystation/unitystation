using System.Collections.Generic;
using System.Linq;

// TODO add Managers namespace
public class JobAllocator
{
	/// <summary>
	/// The default occupation to allocate to players who don't get any jobs.
	/// </summary>
	private static Occupation DefaultJob => OccupationList.Instance.Get(JobType.ASSISTANT);
	/// <summary>
	/// All players who have been allocated a job already
	/// </summary>
	private List<PlayerSpawnRequest> determinedPlayers;
	/// <summary>
	/// Players who haven't been allocated a job yet
	/// </summary>
	private List<ConnectedPlayer> playersLeft;
	/// <summary>
	/// Players who missed out on their job preference
	/// </summary>
	private List<ConnectedPlayer> missedOutPlayers;

	private Dictionary<Occupation, int> occupationCount = new Dictionary<Occupation, int>();

	/// <summary>
	/// Randomly determine the occupations for every player according to their job preferences
	/// </summary>
	/// <param name="players">The players to assign jobs to</param>
	/// <returns>A list of JoinedViewers with the JobTypes assigned to them</returns>
	public List<PlayerSpawnRequest> DetermineJobs(IEnumerable<ConnectedPlayer> players)
	{
		// Reset all player lists
		playersLeft = players.ToList();
		missedOutPlayers = new List<ConnectedPlayer>();
		determinedPlayers = new List<PlayerSpawnRequest>();

		// Find all head jobs and normal jobs
		var headJobs = DepartmentList.Instance.GetAllHeadJobs().ToArray();
		var normalJobs = DepartmentList.Instance.GetAllNormalJobs().ToArray();

		// Allocate jobs from High to Low priority
		var priorityOrder = new [] {Priority.High, Priority.Medium, Priority.Low};
		foreach (var priority in priorityOrder)
		{
			if (!playersLeft.Any())
			{
				// Everyone has been allocated a job so stop checking
				break;
			}

			// Allocate players who didn't get their previous priority choice first
			if (missedOutPlayers.Any())
			{
				ChoosePlayers(headJobs, priority, missedOutPlayers);
				ChoosePlayers(normalJobs, priority, missedOutPlayers);
			}

			// Start by allocating head jobs, then normal jobs
			ChoosePlayers(headJobs, priority, playersLeft);
			ChoosePlayers(normalJobs, priority, playersLeft);
		}

		// TODO: check if head jobs are unassigned and see if a normal job from that department could be promoted
		// Dictionary<Department, ConnectedPlayer> assignedHeadJobs;
		// Dictionary<Department, ConnectedPlayer> assignedNormalJobs;

		AllocateDefaultJobs();
		return determinedPlayers;
	}

	/// <summary>
	/// Chooses players for a collection of occupations based on their job and priority preferences.
	/// Updates the determined players, players left and missed out players while doing so.
	/// </summary>
	/// <param name="occupations">The occupations to assign to players</param>
	/// <param name="priority">The priority to check for</param>
	/// <param name="playerPool">The available players to choose from</param>
	private void ChoosePlayers(IEnumerable<Occupation> occupations, Priority priority,
		IReadOnlyCollection<ConnectedPlayer> playerPool)
	{
		foreach (var occupation in occupations)
		{
			occupationCount.TryGetValue(occupation, out int filledSlots);
			int slotsLeft = occupation.Limit - filledSlots;

			// Check slots leftr and find any players that selected the job with the specified priority
			if (slotsLeft < 1 ||
				!TryGetCandidates(ref playerPool, occupation, priority, out var candidates))
			{
				// Skip this job since all slots for this occupation have been allocated or no candidates available
				continue;
			}

			List<ConnectedPlayer> chosen;
			if (candidates.Count > slotsLeft)
			{
				// More candidates than job slots, choose people randomly to fill all slots
				chosen = candidates.PickRandom(slotsLeft).ToList();

				// Store the players who missed out on their first choice to give them priority for other jobs
				missedOutPlayers.AddRange(candidates.Except(chosen));
			}
			else
			{
				// Equal or less candidates than job slots, add all candidates
				chosen = candidates;
			}

			AllocateJobs(chosen, occupation);

			// No more players left to assign
			if (!playersLeft.Any())
			{
				return;
			}
		}
	}

	/// <summary>
	/// Tries to get candidates from a pool of players for an occupation at a certain priority level.
	/// </summary>
	/// <param name="playerPool">The pool of players to get candidates from</param>
	/// <param name="occupation">The occupation to check</param>
	/// <param name="priority">The priority level to check</param>
	/// <param name="candidates">A list of candidates if any were found</param>
	/// <returns>Returns true if candidates were found, and false if not.</returns>
	private bool TryGetCandidates(ref IReadOnlyCollection<ConnectedPlayer> playerPool, Occupation occupation,
		Priority priority, out List<ConnectedPlayer> candidates)
	{
		// Find any players that selected the job with the specified priority
		candidates = playerPool.Where(player =>
			player.CharacterSettings.JobPreferences.ContainsKey(occupation.JobType) &&
			player.CharacterSettings.JobPreferences[occupation.JobType] == priority && PlayerList.Instance.FindPlayerJobBanEntry(player, occupation.JobType, false) == null).ToList();

		return candidates.Any();
	}

	/// <summary>
	/// Allocates a job to players. Updates _determinedPlayers and _playersLeft.
	/// </summary>
	/// <param name="players">Players to allocate jobs to</param>
	/// <param name="job">The job to allocate</param>
	private void AllocateJobs(IReadOnlyCollection<ConnectedPlayer> players, Occupation job)
	{
		// Update determined players and players left
		determinedPlayers.AddRange(players.Select(player =>
			PlayerSpawnRequest.RequestOccupation(player.ViewerScript, job, player.CharacterSettings, player.UserId)));
		playersLeft.RemoveAll(players.Contains);
		missedOutPlayers.RemoveAll(players.Contains);

		// Update occupation counts
		occupationCount.TryGetValue(job, out int currentCount);
		occupationCount[job] = currentCount + 1;
	}

	/// <summary>
	/// Ensures all players have been allocated a job and allocates the default one
	/// </summary>
	private void AllocateDefaultJobs()
	{
		if (playersLeft.Any())
		{
			Logger.LogFormat("These people were not allocated a job, assigning them to {0}: {1}", Category.Jobs,
				DefaultJob.DisplayName, string.Join("\n", playersLeft));

			// Update determined players and players left
			AllocateJobs(playersLeft, DefaultJob);
		}

		if (missedOutPlayers.Any() || playersLeft.Any())
		{
			Logger.LogError("There are still unallocated players, something has gone wrong in the JobAllocator!",
				Category.Jobs);
		}
	}
}
