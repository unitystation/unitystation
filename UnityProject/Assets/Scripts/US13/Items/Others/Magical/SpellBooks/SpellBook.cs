using Logs;
using UnityEngine;
using US13.Core.Chat;
using US13.Items.Bureaucracy;
using US13.Managers;
using US13.Systems.Antagonists.Antags;
using US13.Systems.Spells;
using Util;

namespace US13.Items.Others.Magical.SpellBooks
{
	/// <summary>
	/// Allows the player to learn the referenced spell when activated.
	/// If the book has already been used, then it will punish the player.
	/// </summary>
	public class SpellBook : SimpleBook
	{
		[Tooltip("The spell to grant to the successful reader.")]
		[SerializeField]
		private SpellData spell = default;

		protected override bool TryReading(PlayerInfo player)
		{
			if (player.Mind.HasSpell(spell))
			{
				if (player.Mind.IsOfAntag<Wizard>())
				{
					Chat.AddExamineMsgFromServer(player.GameObject,
							"You're already far more versed in this spell than this flimsy how-to book can provide!");
				}
				else
				{
					Chat.AddExamineMsgFromServer(player.GameObject, "You already know this spell!");
				}

				return false;
			}

			if (base.TryReading(player) == false)
			{
				Chat.AddActionMsgToChat(gameObject, default, $"The {gameObject.ExpensiveName()} glows in a black light!");
				Punish(player);
				return false;
			}

			return true;
		}

		protected override void FinishReading(PlayerInfo player)
		{
			LearnSpell(player);
			base.FinishReading(player);

			if (AllowOnlyOneReader && hasBeenRead)
			{
				Chat.AddCombatMsgToChat(gameObject, default, $"The {gameObject.ExpensiveName()} glows dark for a second!");
			}
		}

		private void LearnSpell(PlayerInfo player)
		{
			// TODO: Play "Blind" SFX once sound freeze is lifted.
			Chat.AddChatMsgToChatServer(player, spell.InvocationMessage, ChatChannel.Local, Loudness.SCREAMING);
			Chat.AddExamineMsgFromServer(player.GameObject, $"You feel like you've experienced enough to cast <b>{spell.Name}</b>!");

			var learnedSpell = spell.AddToPlayer(player.Mind);
			player.Mind.AddSpell(learnedSpell);
		}

		private void Punish(PlayerInfo player)
		{
			if (gameObject.TryGetComponent<SpellBookPunishment>(out var punishment))
			{
				punishment.Punish(player);
			}
			else
			{
				Loggy.Warning($"No punishment found for {this}!", Category.Spells);
			}
		}
	}
}
