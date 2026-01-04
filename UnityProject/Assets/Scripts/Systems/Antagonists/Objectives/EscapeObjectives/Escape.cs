using System.Collections.Generic;
using System.Linq;
using Antagonists;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Antagonists.Objectives.EscapeObjectives
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

		[BoxGroup("Escape Settings")]
		[Tooltip("If true, the player must be alive (and their body/mind present) to complete this objective")]
		public bool MustBeAlive = true;

		[BoxGroup("Escape Settings")]
		[Tooltip("If true, all escape shuttles during the setup phase will be added to the valid shuttles list " +
		         "instead of just the primary shuttle that is assigned in the GameManager")]
		public bool GrabAllEscapeShuttlesOnSetup = false;

		/// <summary>
		/// Populate the list of valid escape shuttles
		/// </summary>
		protected override void Setup()
		{
			if (GrabAllEscapeShuttlesOnSetup)
			{
				ValidShuttles.AddRange(FindObjectsByType<EscapeShuttle>(FindObjectsSortMode.None));
			}
			else
			{
				ValidShuttles.Add(GameManager.Instance.PrimaryEscapeShuttle);
			}
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
			if (Owner == null || Owner.Body == null)
			{
				// Maybe they got gibbed, or an admin nuked them from the server?
				return false;
			}
			if (MustBeAlive && Owner.Body.IsDeadOrGhost)
			{
				Chat.AddExamineMsg(Owner.gameObject, "Your body is considered dead, you did not escape successfully.");
				return false;
			}

			DynamicItemStorage dynamicItemStorage = Owner.Body.GetComponent<DynamicItemStorage>();

			//for whatever reason this is null, give the guy the greentext
			if (dynamicItemStorage == null) return true;

			foreach (var handCuffs in dynamicItemStorage.GetNamedItemSlots(NamedSlot.handcuffs))
			{
				if (handCuffs.IsEmpty) continue;

				//If any hands are cuff then we fail
				Chat.AddExamineMsg(Owner.gameObject, "Your hands are cuffed, you have not escaped successfully.");
				return false;
			}

			return ValidShuttles.Any(
				shuttle => shuttle.MatrixInfo != null && CheckOnShip(Owner.Body.RegisterPlayer, shuttle.MatrixInfo.Matrix)
				);
		}
	}
}