using UnityEngine;

namespace US13.Systems.Antagonists.Objectives
{
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/BlobDestroyStation")]
	public class BlobDestroyStation : Objective
	{
		protected override void Setup()
		{

		}

		protected override bool CheckCompletion()
		{
			//Done in BlobPlayer.cs victory method
			return false;
		}
	}
}
