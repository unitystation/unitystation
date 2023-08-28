using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// Acquire x amount of spells.
	/// </summary>
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/SurvivalistMagic")]
	public class SurvivalistMagic : Objective
	{
		[SerializeField]
		private int minimumSpellsNeeded = 1;

		protected override void Setup() { }

		protected override void SetupInGame()
		{
			// Pick a random item and add it to the targeted list
			if (attributes[0] is ObjectiveAttributeNumber targetNumber)
			{
				minimumSpellsNeeded = targetNumber.number;
			}
		}

		protected override bool CheckCompletion()
		{
			return Owner.Spells.Count >= minimumSpellsNeeded;
		}
	}
}
