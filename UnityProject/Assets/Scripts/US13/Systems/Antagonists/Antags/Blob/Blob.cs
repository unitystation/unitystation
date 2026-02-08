using UnityEngine;
using US13.Player;

namespace US13.Systems.Antagonists.Antags.Blob
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
