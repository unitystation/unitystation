using System.Collections;
using System.Collections.Generic;
using Blob;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/BlobDestroyStation")]
	public class BlobDestroyStation : Objective
	{
		protected override void Setup()
		{
			if (Owner.body.gameObject.GetComponent<BlobStarter>() == null)
			{
				Owner.body.gameObject.AddComponent<BlobStarter>();
			}
		}

		protected override bool CheckCompletion()
		{
			//Done in BlobPlayer.cs victory method
			return false;
		}
	}
}
