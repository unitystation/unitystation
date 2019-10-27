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
		/// Make sure there's more than one player for this objective
		/// </summary>
		public override bool IsPossible()
		{
			return (PlayerList.Instance.InGamePlayers.Count > 1);
		}

		/// <summary>
		/// Select the target randomly
		/// </summary>
		public override void Setup()
		{
			// Get all ingame players excluding the one who owns this objective, and pick a random one as the target
			List<ConnectedPlayer> playerPool = PlayerList.Instance.InGamePlayers.Where( p => p.Script != Owner.body).ToList();
			int randIndex = Random.Range(0, playerPool.Count);
			Target = playerPool[randIndex].Script;
			description = $"Assassinate {Target.playerName}, the {Target.mind.jobType.JobString()}";
		}

		public override bool IsComplete()
		{
			return (Target.playerHealth == null || Target.playerHealth.IsDead);
		}
	}
}