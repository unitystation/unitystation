using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// An escape objective to escape on the shuttle alive
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/Escape")]
	public class Escape : Objective
	{
		/// <summary>
		/// The shuttles that will be checked for this objective
		/// </summary>
		private List<EscapeShuttle> ValidShuttles = new List<EscapeShuttle>();

		/// <summary>
		/// Populate the list of valid escape shuttles
		/// </summary>
		protected override void Setup()
		{
			ValidShuttles.Add(GameManager.Instance.PrimaryEscapeShuttle);
		}

		private bool CheckOnShip(RegisterTile antagTile, RegisterTile shuttleTile)
		{
			if (antagTile.Matrix.Id == shuttleTile.Matrix.MatrixInfo.Id) return true;
			return shuttleTile.Matrix.HasTile(antagTile.WorldPositionServer, true);
		}

		/// <summary>
		/// Complete if the player is alive and on one of the escape shuttles and shuttle has
		/// at least one working engine
		/// </summary>
		protected override bool CheckCompletion()
		{
			return !Owner.body.playerHealth.IsDead &&
				ValidShuttles.Any( shuttle => shuttle.MatrixInfo != null
					&& (CheckOnShip(Owner.body.registerTile, shuttle.gameObject.RegisterTile())) && shuttle.HasWorkingThrusters);
		}
	}
}