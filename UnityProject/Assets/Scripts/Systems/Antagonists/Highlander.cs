using Antagonists;
using UnityEngine;

namespace Systems.Antagonists
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Highlander")]
	public class Highlander : Antagonist
	{

		public override void AfterSpawn(ConnectedPlayer player)
		{
			player.Script.playerHealth.EnableFastRegen();
		}
	}
}