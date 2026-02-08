using UnityEngine;

namespace US13.Systems.Antagonists.Objectives
{
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/DnaObjective")]
	public class DnaObjective : Objective
	{
		[SerializeField] private int dnaNeedCount = 7;

		protected override bool CheckCompletion()
		{
			if (Owner?.Body?.Changeling?.ExtractedDna == null) return false;
			return Owner?.Body?.Changeling?.ExtractedDna >= dnaNeedCount;
		}

		protected override void Setup()
		{
			description = string.Format(description, dnaNeedCount);
		}

		public override string GetDescription()
		{
			return $"Extract DNA by using your abilities.";
		}

		protected override void SetupInGame()
		{
			dnaNeedCount = attributes[0].Number;
			description = $"Extract {dnaNeedCount} DNA by using your abilities.";
		}
	}
}