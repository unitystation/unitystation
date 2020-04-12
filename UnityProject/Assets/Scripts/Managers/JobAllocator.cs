using System.Collections.Generic;
using System.Linq;

// TODO add Managers namespace
public static class JobAllocator
{
	/// <summary>
	/// The default occupation to allocate to players who don't get any jobs.
	/// </summary>
	private static Occupation DefaultJob => OccupationList.Instance.Get(JobType.ASSISTANT);
	/// <summary>
	/// All players who have been allocated a job already
	/// </summary>
	private static List<PlayerSpawnRequest> _determinedPlayers;
	/// <summary>
	/// Players who haven't been allocated a job yet
	/// </summary>
	private static List<ConnectedPlayer> _playersLeft;
	/// <summary>
	/// Players who missed out on their job preference
	/// </summary>
	private static List<ConnectedPlayer> _missedOutPlayers;

	private static Dictionary<Occupation, int> _occupationCount = new Dictionary<Occupation, int>();

	/// <summary>
	/// Randomly determine the occupations for every player according to their job preferences
	/// </summary>
	/// <param name="players">The players to assign jobs to</param>
	/// <returns>A list of JoinedViewers with the JobTypes assigned to them</returns>
	public static IEnumerable<PlayerSpawnRequest> DetermineJobs(IEnumerable<ConnectedPlayer> players)
	{
		// Reset all player lists
		_playersLeft = players.ToList();
		_missedOutPlayers = new List<ConnectedPlayer>();
		_determinedPlayers = new List<PlayerSpawnRequest>();

		// Find all head jobs and normal jobs
		var headJobs = DepartmentList.Instance.GetAllHeadJobs().ToArray();
		var normalJobs = DepartmentList.Instance.GetAllNormalJobs().ToArray();

		// Allocate jobs from High to Low priority
		var priorityOrder = new [] {Priority.High, Priority.Medium, Priority.Low};
		foreach (var priority in priorityOrder)
		{
			if (!_playersLeft.Any())
			{
				// Everyone has been allocated a job so stop checking
				break;
			}

			// Allocate players who didn't get their previous priority choice first
			if (_missedOutPlayers.Any())
			{
				// var thisPriority = missedOutPlayers.Where(player =>
				// 	player.CharacterSettings.JobPreferences.ContainsValue(priority)).Select(p => );
				//
				//
				// foreach (var player in missedOutPlayers)
				// {
				// 	// TODO!!
				// 	var player.CharacterSettings.JobPreferences
				// 	// Find any players that selected the job with the specified priority
				// 	var candidates = playersLeft.Where(player =>
				// 		player.CharacterSettings.JobPreferences.ContainsValue(Priority.Medium)).ToList();
				// }
				ChoosePlayers(headJobs, priority, _missedOutPlayers);
				ChoosePlayers(normalJobs, priority, _missedOutPlayers);
			}

			// Start by allocating head jobs, then normal jobs
			ChoosePlayers(headJobs, priority, _playersLeft);
			ChoosePlayers(normalJobs, priority, _playersLeft);
		}

		// TODO: check if head jobs are unassigned and see if a normal job from that department could be promoted
		// Dictionary<Department, ConnectedPlayer> assignedHeadJobs;
		// Dictionary<Department, ConnectedPlayer> assignedNormalJobs;

		AllocateDefaultJobs();
		return _determinedPlayers;
	}

	/// <summary>
	/// Chooses players for a collection of occupations based on their job and priority preferences.
	/// Updates the determined players, players left and missed out players while doing so.
	/// </summary>
	/// <param name="occupations">The occupations to assign to players</param>
	/// <param name="priority">The priority to check for</param>
	/// <param name="playerPool">The available players to choose from</param>
	private static void ChoosePlayers(IEnumerable<Occupation> occupations, Priority priority,
		IReadOnlyCollection<ConnectedPlayer> playerPool)
	{
		foreach (var occupation in occupations)
		{
			// No more players left to assign
			if (!_playersLeft.Any())
			{
				return;
			}

			_occupationCount.TryGetValue(occupation, out int filledSlots);
			int slotsLeft = occupation.Limit - filledSlots;

			// All slots for this occupation have been allocated
			if (slotsLeft < 1)
			{
				continue;
			}

			// Find any players that selected the job with the specified priority
			var candidates = playerPool.Where(player =>
				player.CharacterSettings.JobPreferences.ContainsKey(occupation.JobType) &&
				player.CharacterSettings.JobPreferences[occupation.JobType] == priority).ToList();

			// Skip this job since no candidates available
			if (!candidates.Any())
			{
				continue;
			}

			List<ConnectedPlayer> chosen;
			if (candidates.Count > slotsLeft)
			{
				// More candidates than job slots, choose people randomly to fill all slots
				chosen = candidates.PickRandom(slotsLeft).ToList();

				// Store the players who missed out on their first choice to give them priority for other jobs
				_missedOutPlayers.AddRange(candidates.Except(chosen));
			}
			else
			{
				// Equal or less candidates than job slots, add all candidates
				chosen = candidates;
			}
			AllocateJobs(chosen, occupation);
		}
	}

	/// <summary>
	/// Allocates a job to players. Updates _determinedPlayers and _playersLeft.
	/// </summary>
	/// <param name="players">Players to allocate jobs to</param>
	/// <param name="job">The job to allocate</param>
	private static void AllocateJobs(IReadOnlyCollection<ConnectedPlayer> players, Occupation job)
	{
		// Update determined players and players left
		_determinedPlayers.AddRange(players.Select(player =>
			PlayerSpawnRequest.RequestOccupation(player.ViewerScript, job, player.CharacterSettings)));
		_playersLeft.RemoveAll(players.Contains);

		// Update occupation counts
		_occupationCount.TryGetValue(job, out int currentCount);
		_occupationCount[job] = currentCount + 1;
	}

	/// <summary>
	/// Ensures all players have been allocated a job and allocates the default one
	/// </summary>
	private static void AllocateDefaultJobs()
	{
		if (_missedOutPlayers.Any() || _playersLeft.Any())
		{
			if (!_missedOutPlayers.Equals(_playersLeft))
			{
				Logger.LogError("Missed out players != players left!", Category.Jobs);
			}

			Logger.LogFormat("These people have missed out on jobs, assigning to Assistant: {0}", Category.Jobs, string.Join(",", _missedOutPlayers));

			// Update determined players and players left
			AllocateJobs(_missedOutPlayers, DefaultJob);
		}

		if (_playersLeft.Any())
		{
			Logger.LogError("There are still some players left!", Category.Jobs);
		}
	}
}
