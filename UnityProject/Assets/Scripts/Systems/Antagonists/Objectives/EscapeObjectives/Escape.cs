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

		private bool CheckOnShip(RegisterPlayer antagTile, Matrix shuttleMatrix)
		{
			return shuttleMatrix.PresentPlayers.Contains(antagTile);
		}

		/// <summary>
		/// Complete if the player is alive and on one of the escape shuttles and shuttle has
		/// at least one working engine
		/// </summary>
		protected override bool CheckCompletion()
		{

			DynamicItemStorage dynamicItemStorage = Owner.body.GetComponent<DynamicItemStorage>();

			//for whatever reason this is null, give the guy the greentext
			if (dynamicItemStorage == null) return true;

			foreach (var handCuffs in dynamicItemStorage.GetNamedItemSlots(NamedSlot.handcuffs))
			{
				if (handCuffs.IsEmpty) continue;

				//If any hands are cuff then we fail
				return false;
			}

			return !Owner.body.playerHealth.IsDead &&
				ValidShuttles.Any( shuttle => shuttle.MatrixInfo != null
					&& (CheckOnShip(Owner.body.RegisterPlayer, shuttle.MatrixInfo.Matrix)) && shuttle.HasWorkingThrusters);
		}
	}
}