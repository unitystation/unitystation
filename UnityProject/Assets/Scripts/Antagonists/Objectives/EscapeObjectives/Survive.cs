using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// An escape objective to survive
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/Survive")]
	public class Survive : Objective
	{
		/// <summary>
		/// Complete if the player is alive
		/// </summary>
		public override bool IsComplete(PlayerNetworkActions player)
		{
			return !player.GetComponent<LivingHealthBehaviour>().IsDead;
		}
	}
}