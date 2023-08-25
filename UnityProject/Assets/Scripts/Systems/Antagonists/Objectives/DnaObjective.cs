using Antagonists;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/DnaObjective")]
	public class DnaObjective : Objective
	{
		[SerializeField] private int dnaNeedCount = 7;

		protected override bool CheckCompletion()
		{
			return Owner.Body.Changeling.ExtractedDna >= dnaNeedCount;
		}

		protected override void Setup()
		{
			objectiveName = string.Format(objectiveName, dnaNeedCount);
			description = string.Format(description, dnaNeedCount);
		}
	}
}