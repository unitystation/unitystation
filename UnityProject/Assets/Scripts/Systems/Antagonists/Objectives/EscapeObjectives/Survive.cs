using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// An escape objective to survive
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/Survive")]
	public class Survive : Objective
	{
		/// <summary>
		/// No setup required
		/// </summary>
		protected override void Setup() {}

		/// <summary>
		/// Complete if the player is alive
		/// </summary>
		protected override bool CheckCompletion()
		{
			return !Owner.body.playerHealth.IsDead;
		}
	}
}