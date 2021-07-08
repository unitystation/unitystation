using System.Collections;
using UnityEngine;

namespace Items.Magical
{
	public class InstantSummonsPunishment : SpellBookPunishment
	{
		public override void Punish(ConnectedPlayer player)
		{
			Chat.AddActionMsgToChat(player.GameObject,
					"<color='red'>The book disappears from your hand!</color>",
					$"<color='red'>The book disappears from {player.Script.visibleName}'s hand!</color>");

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
