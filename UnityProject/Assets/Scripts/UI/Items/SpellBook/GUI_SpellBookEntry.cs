using UnityEngine;
using UI.Core.NetUI;
using ScriptableObjects.Items.SpellBook;
using Systems.Spells;
using UI.Objects.Robotics;

namespace UI.SpellBook
{
	public class GUI_SpellBookEntry : DynamicEntry
	{
		[SerializeField]
		private NetText_label spellLabel = default;
		[SerializeField]
		private NetText_label costLabel = default;
		[SerializeField]
		private NetText_label cooldownLabel = default;
		[SerializeField]
		private NetText_label descriptionLabel = default;
		[SerializeField]
		private NetText_label noteLabel = default;
		[SerializeField]
		private NetText_label buttonLabel = default;
		[SerializeField]
		private NetInteractiveButton button = default;

		private GUI_SpellBook bookGUI;
		private SpellBookEntry entry;

		public void SetValues(GUI_SpellBook bookGUI, SpellBookEntry entry)
		{
			this.bookGUI = bookGUI;
			this.entry = entry;

			costLabel.SetValueServer($"Cost: {entry.Cost}");
			descriptionLabel.SetValueServer(entry.Description);

			if (entry is SpellBookSpell spellEntry)
			{
				SetSpellValues(spellEntry);
			}
			else if (entry is SpellBookArtifact artifactEntry)
			{
				SetArtifactValues(artifactEntry);
			}
			else if (entry is SpellBookRitual ritualEntry)
			{
				SetRitualValues(ritualEntry);
			}

			// Enable or disable button interactivity based on affordability.
			button.SetValueServer(entry.Cost > bookGUI.Points ? "false" : "true");
		}

		private void SetSpellValues(SpellBookSpell spellEntry)
		{
			Spell readerSpell = bookGUI.GetReaderSpellInstance(spellEntry.Spell);
			if (readerSpell == null)
			{
				spellLabel.SetValueServer(spellEntry.Name);
				cooldownLabel.SetValueServer($"Cooldown: {spellEntry.Spell.CooldownTime}");
				buttonLabel.SetValueServer("Learn");
			}
			else
			{
				spellLabel.SetValueServer($"{spellEntry.Name} {readerSpell.CurrentTier + 1}");
				cooldownLabel.SetValueServer("Cooldown: " +
					(readerSpell.CooldownTime - (readerSpell.CooldownTime * spellEntry.Spell.CooldownModifier)).ToString("G2"));
				buttonLabel.SetValueServer("Upgrade");
			}

			noteLabel.SetValueServer(
					$"{(spellEntry.RequiresWizardGarb ? "Requires wizard garb" : "Can be cast without wizard garb")} {spellEntry.Note}");
		}

		private void SetArtifactValues(SpellBookArtifact artifactEntry)
		{
			spellLabel.SetValueServer(artifactEntry.Name);
			cooldownLabel.SetValueServer(default);
			noteLabel.SetValueServer(artifactEntry.Note);
			buttonLabel.SetValueServer("Summon");
		}

		private void SetRitualValues(SpellBookRitual ritualEntry)
		{
			spellLabel.SetValueServer(ritualEntry.Name);
			cooldownLabel.SetValueServer(default);
			noteLabel.SetValueServer(ritualEntry.Note);
			buttonLabel.SetValueServer("Cast");
		}

		public void Activate()
		{
			bookGUI.ActivateEntry(entry);
		}
	}
}
