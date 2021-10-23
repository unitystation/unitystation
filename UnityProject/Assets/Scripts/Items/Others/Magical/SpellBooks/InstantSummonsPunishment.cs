using UnityEngine;
using AddressableReferences;


namespace Items.Magical
{
	public class InstantSummonsPunishment : SpellBookPunishment
	{
		[SerializeField]
		private AddressableAudioSource punishSfx = default;

		public override void Punish(ConnectedPlayer player)
		{
			SoundManager.PlayNetworkedAtPos(punishSfx, player.Script.WorldPos, sourceObj: player.GameObject);
			Chat.AddActionMsgToChat(player.GameObject,
					"<color=red>The book disappears from your hand!</color>",
					$"<color=red>The book disappears from {player.Script.visibleName}'s hand!</color>");

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
