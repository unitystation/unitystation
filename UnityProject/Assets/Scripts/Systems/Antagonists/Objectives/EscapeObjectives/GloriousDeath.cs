using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// Die in a glorious death
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/GloriousDeath")]
	public class GloriousDeath : Objective
	{
		/// <summary>
		/// No setup required
		/// </summary>
		protected override void Setup() {}

		/// <summary>
		/// Complete if the player is dead
		/// </summary>
		protected override bool CheckCompletion()
		{
			return Owner.body.IsDeadOrGhost;
		}
	}
}
