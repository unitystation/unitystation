using Antagonists;
using UnityEngine;

namespace Systems.Antagonists
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Highlander")]
	public class Highlander : Antagonist
	{

		public override void AfterSpawn(PlayerInfo player)
		{
			player.Script.playerHealth.EnableFastRegen();
			player.Script.characterSettings.Speech = Speech.Scotsman;
		}
	}
}