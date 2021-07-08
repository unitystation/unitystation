using System.Collections.Generic;
using System.Linq;
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
		private PlayerScript Target;

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
			Target = playerPool.PickRandom().Script;

			//If still null then its a free objective
			if(Target == null || Target.mind.occupation == null)
			{
				FreeObjective();
				return;
			}

			AntagManager.Instance.TargetedPlayers.Add(Target);
			description = $"Prevent {Target.playerName}, the {Target.mind.occupation.DisplayName} from leaving the station";

			ValidShuttles.Add(GameManager.Instance.PrimaryEscapeShuttle);
		}

		private void FreeObjective()
		{
			Logger.LogWarning("Unable to find any suitable maroon targets! Giving free objective", Category.Antags);
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
			if (Target.IsDeadOrGhost)
			{
				return true;
			}

			//If target is on functional escape shuttle, we failed
			return ValidShuttles.Any( shuttle => shuttle.MatrixInfo != null
				&& Target.registerTile.Matrix.Id == shuttle.MatrixInfo.Id && shuttle.HasWorkingThrusters) == false;
		}
	}
}
