using UnityEngine;
using Blob;
using Player;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Blob")]
	public class Blob : Antagonist
	{
		public override void AfterSpawn(Mind NewMind)
		{
			//Add blob player to game object
			NewMind.Body.gameObject.AddComponent<BlobStarter>();
		}
	}
}
