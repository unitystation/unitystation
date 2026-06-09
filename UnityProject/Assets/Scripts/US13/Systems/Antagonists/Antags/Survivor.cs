using UnityEngine;
using US13.Player;

namespace US13.Systems.Antagonists.Antags
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Survivor")]
	public class Survivor : Antagonist
	{

		public override void AfterSpawn(Mind player) { }
	}
}
