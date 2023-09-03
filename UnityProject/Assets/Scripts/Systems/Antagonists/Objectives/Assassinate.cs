using System.Collections.Generic;
using System.Linq;
using Logs;
using Systems.Antagonists.Antags;
using UnityEngine;
using Systems.GhostRoles;

namespace Antagonists
{
	/// <summary>
	/// An objective to assassinate someone on the station
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/Assassinate")]
	public class Assassinate : Objective
	{
		/// <summary>
		/// The person to assassinate
		/// </summary>
		private Mind Target;

		/// <summary>
		/// Make sure there's at least one player which hasn't been targeted, not including the candidate
		/// </summary>
		protected override bool IsPossibleInternal(Mind candidate)
		{
			var players = PlayerList.Instance.InGamePlayers.FindAll(x => x.Mind != null);
			int targetCount = players.Count(p => (p.Mind != candidate)
			                                     && AntagManager.Instance.TargetedPlayers.Contains(p.Mind) == false
			                                     && p.Mind.GetAntag()?.Antagonist is not BloodBrother);
			return (targetCount > 0);
		}

		/// <summary>
		/// Select the target randomly (not including Owner or other targeted players)
		/// </summary>
		protected override void Setup()
		{
			// Get all ingame players except the one who owns this objective and players who have already been targeted and the ones who cant be targeted
			List<PlayerInfo> playerPool = PlayerList.Instance.InGamePlayers.Where( p =>
				(p.Script != Owner.Body) && !AntagManager.Instance.TargetedPlayers.Contains(p.Mind) && p.Mind.occupation != null && p.Mind.occupation.IsTargeteable

			).ToList();

			if (playerPool.Count == 0)
			{
				FreeObjective();
				return;
			}

			// Pick a random target and add them to the targeted list
			Target = playerPool.PickRandom().Mind;

			//If still null then its a free objective
			if(Target == null || Target.occupation == null)
			{
				FreeObjective();
				return;
			}

			AntagManager.Instance.TargetedPlayers.Add(Target);
			description = $"Assassinate {Target.name}, the {Target.occupation.DisplayName}";
		}

		private void FreeObjective()
		{
			Loggy.LogWarning("Unable to find any suitable assassination targets! Giving free objective", Category.Antags);
			description = "Free objective";
			Complete = true;
		}

		protected override bool CheckCompletion()
		{
			if (Target == null) return false;
			if (IsGhostRole(Target.occupation.JobType)) return false;
			return Target.Body.playerHealth == null || Target.Body.IsDeadOrGhost;
		}

		private static bool IsGhostRole(JobType playerJob)
		{
			foreach (var roleData in GhostRoleManager.Instance.GhostRoles)
			{
				if (roleData.TargetOccupation == null) continue;
				if (playerJob == roleData.TargetOccupation.JobType) return true;
			}

			return false;
		}
	}
}
