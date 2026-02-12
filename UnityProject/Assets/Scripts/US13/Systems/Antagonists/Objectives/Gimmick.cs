using UnityEngine;

namespace US13.Systems.Antagonists.Objectives
{
	/// <summary>
	/// Gimmick objective, always completed
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/Gimmick")]
	public class Gimmick : Objective
	{
		protected override void Setup(){ }

		protected override bool CheckCompletion()
		{
			//Always true
			return true;
		}
	}
}

