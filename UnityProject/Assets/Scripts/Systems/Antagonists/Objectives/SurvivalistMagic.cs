using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;

namespace Antagonists
{
	/// <summary>
	/// Acquire x amount of spells.
	/// </summary>
	[CreateAssetMenu(menuName = "ScriptableObjects/Objectives/SurvivalistMagic")]
	public class SurvivalistMagic : Objective
	{
		[SerializeField]
		private int minimumSpellsNeeded = 1;

		protected override void Setup()
		{
		}

		protected override bool CheckCompletion()
		{
			return Owner.Spells.Count > 0;
		}
	}
}
