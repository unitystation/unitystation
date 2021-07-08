using System.Collections;
using System.Collections.Generic;
using Blob;
using UnityEngine;

namespace Antagonists
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
