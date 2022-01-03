using System.Collections.Generic;
using System.Linq;
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
		private ConnectedPlayer Target;

		/// <summary>
		/// Make sure there's at least one player which hasn't been targeted, not including the candidate
		/// </summary>
		protected override bool IsPossibleInternal(PlayerScript candidate)
		{
			int targetCount = PlayerList.Instance.InGamePlayers.Where( p =>
				(p.Script != candidate) && !AntagManager.Instance.TargetedPlayers.Contains(p.Script)
			).Count();
			return (targetCount > 0);
		}

		/// <summary>
		/// Select the target randomly (not including Owner or other targeted players)
		/// </summary>
		protected override void Setup()
		{
			// Get all ingame players except the one who owns this objective and players who have already been targeted and the ones who cant be targeted
			List<ConnectedPlayer> playerPool = PlayerList.Instance.InGamePlayers.Where( p =>
				(p.Script != Owner.body) && !AntagManager.Instance.TargetedPlayers.Contains(p.Script) && p.Script.mind.occupation != null && p.Script.mind.occupation.IsTargeteable

			).ToList();

			if (playerPool.Count == 0)
			{
				FreeObjective();
				return;
			}

			// Pick a random target and add them to the targeted list
			Target = playerPool.PickRandom().Script.connectedPlayer;

			//If still null then its a free objective
			if(Target == null || Target.Script.mind.occupation == null)
			{
				FreeObjective();
				return;
			}

			AntagManager.Instance.TargetedPlayers.Add(Target.Script);
			description = $"Assassinate {Target.Script.playerName}, the {Target.Script.mind.occupation.DisplayName}";
		}

		private void FreeObjective()
		{
			Logger.LogWarning("Unable to find any suitable assassination targets! Giving free objective", Category.Antags);
			description = "Free objective";
			Complete = true;
		}

		protected override bool CheckCompletion()
		{
			if (Target == null || Target.Script == null) return false;
			if (IsGhostRole(Target.Job)) return false;
			return Target.Script.playerHealth == null || Target.Script.IsDeadOrGhost;
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
