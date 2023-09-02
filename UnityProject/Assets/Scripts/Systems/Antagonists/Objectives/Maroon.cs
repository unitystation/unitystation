using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// Maroon your target on the station (dont allow them to leave the station on the shuttle)
	/// Basically a slightly different assassinate objective
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/Maroon")]
	public class Maroon : Objective
	{
		/// <summary>
		/// The shuttles that will be checked for this objective
		/// </summary>
		private List<EscapeShuttle> ValidShuttles = new List<EscapeShuttle>();

		/// <summary>
		/// The person to assassinate
		/// </summary>
		private Mind Target;

		/// <summary>
		/// Make sure there's at least one player which hasn't been targeted, not including the candidate
		/// </summary>
		protected override bool IsPossibleInternal(Mind candidate)
		{
			int targetCount = PlayerList.Instance.InGamePlayers.Count(p => (p.Mind != candidate) && !AntagManager.Instance.TargetedPlayers.Contains(p.Mind));
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
			description = $"Prevent {Target.name}, the {Target.occupation.DisplayName} from leaving the station";

			ValidShuttles.Add(GameManager.Instance.PrimaryEscapeShuttle);
		}

		private void FreeObjective()
		{
			Loggy.LogWarning("Unable to find any suitable maroon targets! Giving free objective", Category.Antags);
			description = "Free objective";
			Complete = true;
		}

		/// <summary>
		/// Complete if the Target is dead or not on a functional emergency shuttle
		/// </summary>
		protected override bool CheckCompletion()
		{
			//If dead then objective complete
			//TODO, maybe change in future to make sure dead body isnt on shuttle either
			if (Target.IsGhosting)
			{
				return true;
			}

			//If target is on functional escape shuttle, we failed
			return ValidShuttles.Any( shuttle => shuttle
				&& shuttle.MatrixInfo.Matrix.PresentPlayers.Contains(Owner.Body.RegisterPlayer) && shuttle.HasWorkingThrusters) == false;
		}
	}
}
