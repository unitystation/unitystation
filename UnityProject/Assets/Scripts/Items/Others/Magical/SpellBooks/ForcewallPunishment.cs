using System.Collections;
using UnityEngine;
using AddressableReferences;


namespace Items.Magical
{
	// TODO: make the player a statue when petrification is added. 

	/// <summary>
	/// Punishes the player by temporarily preventing movement input and removing player speech.
	/// </summary>
	public class ForcewallPunishment : SpellBookPunishment
	{
		[SerializeField, Range(1, 300)]
		private int petrifyTime = 60;

		[SerializeField]
		private AddressableAudioSource punishSfx = default;

		public override void Punish(ConnectedPlayer player)
		{
			Chat.AddCombatMsgToChat(player.GameObject,
					"You suddenly feel very solid!",
					$"{player.GameObject.ExpensiveName()} goes very still! {player.Script.characterSettings.TheyPronoun(player.Script)}'s been petrified!");

			player.Script.playerMove.allowInput = false;
			// Piggy-back off IsMiming property to prevent the player from speaking.
			// TODO: convert to player trait when we have that system.
			player.Script.mind.IsMiming = true;

			StartCoroutine(Unpetrify(player.Script));

			SoundManager.PlayNetworkedAtPos(punishSfx, player.Script.WorldPos, sourceObj: player.GameObject);
			Chat.AddCombatMsgToChat(player.GameObject,
					"<size=60><b>Your body freezes up! Can't... move... can't... think...</b></size>",
					$"{player.GameObject.ExpensiveName()}'s skin rapidly turns to marble!");
			
		}

		private IEnumerator Unpetrify(PlayerScript script)
		{
			yield return WaitFor.Seconds(petrifyTime);
			if (script == null || script.mind == null) yield break;

			script.playerMove.allowInput = true;
			script.mind.IsMiming = false;

			Chat.AddExamineMsgFromServer(script.gameObject, "You feel yourself again.");
		}
	}
}
