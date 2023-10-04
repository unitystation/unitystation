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
			description = string.Format(description, dnaNeedCount);
		}

		public override string GetDescription()
		{
			return $"Extract {dnaNeedCount} DNA by using your abilities.";
		}

		protected override void SetupInGame()
		{	
			dnaNeedCount = attributes[0].number;
			description = $"Extract {dnaNeedCount} DNA by using your abilities.";
		}
	}
}