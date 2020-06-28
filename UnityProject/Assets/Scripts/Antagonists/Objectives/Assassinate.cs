using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// An objective to assassinate someone on the station
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/Assassinate")]
	public class Assassinate : Objective
	{

		/// <summary>
		/// The person to assassinate
		/// </summary>
		private PlayerScript Target;

		/// <summary>
		/// Make sure there's at least one player which hasn't been targeted, not including the candidate
		/// </summary>
		public override bool IsPossible(PlayerScript candidate)
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
			// Get all ingame players except the one who owns this objective and players who have already been targeted
			List<ConnectedPlayer> playerPool = PlayerList.Instance.InGamePlayers.Where( p =>
				(p.Script != Owner.body) && !AntagManager.Instance.TargetedPlayers.Contains(p.Script)
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
			description = $"Assassinate {Target.playerName}, the {Target.mind.occupation.DisplayName}";
		}

		private void FreeObjective()
		{
			Logger.LogWarning("Unable to find any suitable assassination targets! Giving free objective", Category.Antags);
			description = "Free objective";
			Complete = true;
		}

		protected override bool CheckCompletion()
		{
			return (Target.playerHealth == null || Target.playerHealth.IsDead);
		}
	}
}