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
	public IEnumerable<PlayerSpawnRequest> DetermineJobs(IEnumerable<ConnectedPlayer> players)
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
			// No more players left to assign
			if (!playersLeft.Any())
			{
				return;
			}

			occupationCount.TryGetValue(occupation, out int filledSlots);
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
				missedOutPlayers.AddRange(candidates.Except(chosen));
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
	private void AllocateJobs(IReadOnlyCollection<ConnectedPlayer> players, Occupation job)
	{
		// Update determined players and players left
		determinedPlayers.AddRange(players.Select(player =>
			PlayerSpawnRequest.RequestOccupation(player.ViewerScript, job, player.CharacterSettings)));
		playersLeft.RemoveAll(players.Contains);

		// Update occupation counts
		occupationCount.TryGetValue(job, out int currentCount);
		occupationCount[job] = currentCount + 1;
	}

	/// <summary>
	/// Ensures all players have been allocated a job and allocates the default one
	/// </summary>
	private void AllocateDefaultJobs()
	{
		if (missedOutPlayers.Any() || playersLeft.Any())
		{
			if (!missedOutPlayers.Equals(playersLeft))
			{
				Logger.LogError("Missed out players != players left! " +
								"Something isn't assigning missed out players correctly.", Category.Jobs);
			}

			Logger.LogFormat("These people were not assigned a job, assigning to {0}: {1}", Category.Jobs,
				DefaultJob.DisplayName, string.Join("\n", playersLeft));

			// Update determined players and players left
			AllocateJobs(playersLeft, DefaultJob);
		}

		if (playersLeft.Any())
		{
			Logger.LogError("There are still some players left!", Category.Jobs);
		}
	}
}
