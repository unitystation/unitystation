using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;
using Items.Bureaucracy;

namespace Items.Magical
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

		protected override bool TryReading(ConnectedPlayer player)
		{
			if (player.Script.mind.HasSpell(spell))
			{
				if (player.Script.mind.IsOfAntag<Antagonists.Wizard>())
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

		protected override void FinishReading(ConnectedPlayer player)
		{
			LearnSpell(player);
			base.FinishReading(player);

			if (AllowOnlyOneReader && hasBeenRead)
			{
				Chat.AddCombatMsgToChat(gameObject, default, $"The {gameObject.ExpensiveName()} glows dark for a second!");
			}
		}

		private void LearnSpell(ConnectedPlayer player)
		{
			// TODO: Play "Blind" SFX once sound freeze is lifted.
			Chat.AddChatMsgToChat(player, spell.InvocationMessage, ChatChannel.Local, Loudness.SCREAMING);
			Chat.AddExamineMsgFromServer(player.GameObject, $"You feel like you've experienced enough to cast <b>{spell.Name}</b>!");

			var learnedSpell = spell.AddToPlayer(player.Script);
			player.Script.mind.AddSpell(learnedSpell);
		}

		private void Punish(ConnectedPlayer player)
		{
			if (gameObject.TryGetComponent<SpellBookPunishment>(out var punishment))
			{
				punishment.Punish(player);
			}
			else
			{
				Logger.LogWarning($"No punishment found for {this}!", Category.Spells);
			}
		}
	}
}
