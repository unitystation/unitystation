using UnityEngine;
using US13.Core.Chat;
using US13.Messages.Server;
using US13.Player;

namespace US13.Systems.Antagonists.Antags
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Cargonian")]
	public class Cargonian : Antagonist
	{

		public override void AfterSpawn(Mind player)
		{
			UpdateChatMessage.Send(player.gameObject, ChatChannel.System, ChatModifier.None,
				"<color=red>Something has awoken in you. You feel the urgent need to rebel " +
				"alongside all your brothers in your department against this station.</color>");
		}
	}
}
