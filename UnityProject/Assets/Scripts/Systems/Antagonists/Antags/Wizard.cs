using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using NaughtyAttributes;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;
using ScriptableObjects;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Wizard")]
	public class Wizard : Antagonist
	{
		[Tooltip("How many random spells the wizard should start with.")]
		[SerializeField]
		private int startingSpellCount = 1;

		[SerializeField, BoxGroup("Starting Name")]
		private bool assignRandomNameOnSpawn = true;
		[SerializeField, BoxGroup("Starting Name")]
		private StringList wizardFirstNames = default;
		[SerializeField, BoxGroup("Starting Name")]
		private StringList wizardLastNames = default;

		public int StartingSpellCount => startingSpellCount;

		public override void AfterSpawn(ConnectedPlayer player)
		{
			GiveRandomSpells(player);

			if (assignRandomNameOnSpawn)
			{
				player.Script.SetPermanentName(GetRandomWizardName());
			}

			SetPapers(player);
		}

		public string GetRandomWizardName()
		{
			return $"{wizardFirstNames.GetRandom()} {wizardLastNames.GetRandom()}";
		}

		public static string GetIdentityPaperText(ConnectedPlayer player)
		{
			return $"<size=36>CERTIFICATE OF IDENTITY</size>\n\n\n" +
					$"This slip is to certify that the bearer,\n" +
					$"<u><b>{player.Script.playerName}</b></u>\n" +
					"is a member of the Wizard Federation.\n\n\n\n\n\n\n\n\n\n\n\n" +
					"Signed: <u><i>Tarkhol Mintizheth</i></u>, Wizard Fedaration Chief Recruiter\n\n" +
					"<size=16>This certificate remains property of the Wizard Federation</size>";
		}

		private void GiveRandomSpells(ConnectedPlayer player)
		{
			if (StartingSpellCount < 1) return;

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

		private void SetPapers(ConnectedPlayer player)
		{
			ItemSlot idSlot = player.Script.ItemStorage.GetNamedItemSlot(NamedSlot.id);
			if (idSlot.IsOccupied && idSlot.ItemObject.TryGetComponent<Paper>(out var papersPlease))
			{
				papersPlease.SetServerString(GetIdentityPaperText(player));
			}

			ItemSlot storage02 = player.Script.ItemStorage.GetNamedItemSlot(NamedSlot.storage02);
			if (storage02.IsOccupied && storage02.ItemObject.TryGetComponent<Paper>(out var helpPaper))
			{
				helpPaper.SetServerString(
						"<align=\"center\"><size=32><b>Wizard 101</b></size></align>\n" +
						"- Use the magic mirror to change your name.\n" +
						"- Avoid the Whizzamazon drop-pod that falls when you purchase an artifact!\n" +
						"- Some spells require wizard garb to cast. Prevent the crew taking them off you.\n" +
						"- On your first mission, act defensively.\n" +
						"- Once you teleport to the station, you cannot return.\n" +
						"- The Blink spell has a small chance to send you into space if you use it while near space.\n" +
						"- The wizard staff does not serve a meaningful purpose, but it does look great on you!\n" +
						"Good luck!");
			}
		}

		private IEnumerable<SpellData> GetRandomWizardSpells()
		{
			return SpellList.Instance.Spells.Where(s => s is WizardSpellData).PickRandom(StartingSpellCount);
		}
	}
}
