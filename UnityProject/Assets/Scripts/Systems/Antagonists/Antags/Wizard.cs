using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Wizard")]
	public class Wizard : Antagonist
	{
		[Tooltip("How many random spells the wizard should start with.")]
		[SerializeField]
		private int startingSpellCount = 1;

		public int StartingSpellCount => startingSpellCount;

		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			var newPlayer = PlayerSpawn.ServerSpawnPlayer(spawnRequest.JoinedViewer, AntagOccupation,
					spawnRequest.CharacterSettings);
			GiveRandomSpells(newPlayer.Player());

			return newPlayer;
		}

		private void GiveRandomSpells(ConnectedPlayer player)
		{
			StringBuilder playerMsg = new StringBuilder("You have knowledge of the following spells: ");

			foreach (WizardSpellData randomSpell in GetRandomWizardSpells())
			{
				Spell spell = randomSpell.AddToPlayer(player.Script);
				player.Script.mind.AddSpell(spell);
				playerMsg.Append($"<b>{randomSpell.Name}</b>, ");
			}
			playerMsg.RemoveLast(", ").Append(".");

			Chat.AddExamineMsgFromServer(player.GameObject, playerMsg.ToString());
		}

		private IEnumerable<SpellData> GetRandomWizardSpells()
		{
			return SpellList.Instance.Spells.Where(s => s is WizardSpellData).PickRandom(StartingSpellCount);
		}
	}
}
