using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/PreventEveryoneFromEscaping")]
	public class PreventEveryoneFromEscaping : Objective
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

		/// <summary>
		/// Complete if the player is alive and on one of the escape shuttles and shuttle has
		/// at least one working engine
		/// </summary>
		protected override bool CheckCompletion()
		{
			return !ValidShuttles.Any( shuttle => shuttle.MatrixInfo != null && CheckForPlayersOnShuttle(shuttle));
		}

		private bool CheckForPlayersOnShuttle(EscapeShuttle shuttle)
		{
			LayerMask layersToCheck = LayerMask.GetMask("Players", "NPC");
			foreach (Transform trans in shuttle.MatrixInfo.Objects)
			{
				if (((1 << trans.gameObject.layer) & layersToCheck) == 0)
				{
					return true;
				}
			}
			return false;
		}
	}
}