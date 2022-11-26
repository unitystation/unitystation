using Antagonists;
using UnityEngine;

namespace Systems.Antagonists
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Highlander")]
	public class Highlander : Antagonist
	{

		public override void AfterSpawn(Mind player)
		{
			player.Body.playerHealth.EnableFastRegen();
			player.CurrentCharacterSettings.Speech = Speech.Scotsman;
		}
	}
}