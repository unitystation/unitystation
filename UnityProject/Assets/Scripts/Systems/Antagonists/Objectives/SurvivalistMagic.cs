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

		protected override bool CheckCompletion()
		{
			return Owner.Spells.Count >= minimumSpellsNeeded;
		}
	}
}
