using UnityEngine;
using US13.Player;
using US13.UI.Systems.Lobby;

namespace US13.Systems.Antagonists
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