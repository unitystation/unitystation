using UnityEngine;
using Player;

namespace Antagonists
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Survivor")]
	public class Survivor : Antagonist
	{

		public override void AfterSpawn(Mind player) { }
	}
}
