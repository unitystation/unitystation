using System.Collections;
using UnityEngine;

namespace Items.Others.Magical
{
	public class InstantSummonsPunishment : SpellBookPunishment
	{
		public override void Punish(ConnectedPlayer player)
		{
			Chat.AddActionMsgToChat(player.GameObject,
					"<color='red'>The book disappears from your hand!</color>",
					$"<color='red'>The book disappears from {player.Script.visibleName}'s hand!</color>");

			Despawn.ServerSingle(gameObject);
		}
	}
}
