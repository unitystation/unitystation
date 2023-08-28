using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Weapons;

namespace Antagonists
{
	/// <summary>
	/// Acquire x amount of guns.
	/// </summary>
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/SurvivalistGuns")]
	public class SurvivalistGuns : Objective
	{
		[SerializeField]
		private int minimumGunsNeeded = 2;

		protected override void Setup()
		{
		}

		protected override void SetupInGame()
		{
			// Pick a random item and add it to the targeted list
			if (attributes[0] is ObjectiveAttributeNumber targetNumber)
			{
				minimumGunsNeeded = targetNumber.number;
			}
		}

		protected override bool CheckCompletion()
		{
			return CheckStorageFor(typeof(Gun), minimumGunsNeeded);
		}
	}
}
