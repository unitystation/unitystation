using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.Managers;

namespace US13.Items.Others.Magical.SpellBooks
{
	public class InstantSummonsPunishment : SpellBookPunishment
	{
		[SerializeField]
		private AddressableAudioSource punishSfx = default;

		public override void Punish(PlayerInfo player)
		{
			SoundManager.PlayNetworkedAtPos(punishSfx, player.Script.WorldPos, sourceObj: player.GameObject);
			Chat.AddActionMsgToChat(player.GameObject,
					"<color=red>The book disappears from your hand!</color>",
					$"<color=red>The book disappears from {player.Script.visibleName}'s hand!</color>");

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
