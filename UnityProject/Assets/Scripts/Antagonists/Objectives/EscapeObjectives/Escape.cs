using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// An escape objective to escape on the shuttle alive
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/Escape")]
	public class Escape : Objective
	{
		/// <summary>
		/// Complete if the player is alive and on the escape shuttle
		/// </summary>
		public override bool IsComplete(PlayerScript player)
		{
			return !player.playerHealth.IsDead &&
				(player.registerTile.Matrix.Id == GameManager.Instance.PrimaryEscapeShuttle.MatrixInfo.Id);
		}
	}
}