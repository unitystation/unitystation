using UnityEngine;
using UI.Core.NetUI;
using ScriptableObjects.Items.SpellBook;
using Systems.Spells;

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

			costLabel.MasterSetValue($"Cost: {entry.Cost}");
			descriptionLabel.MasterSetValue(entry.Description);

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
			button.MasterSetValue(entry.Cost > bookGUI.Points ? "false" : "true");
		}

		private void SetSpellValues(SpellBookSpell spellEntry)
		{
			Spell readerSpell = bookGUI.GetReaderSpellInstance(spellEntry.Spell);
			if (readerSpell == null)
			{
				spellLabel.MasterSetValue(spellEntry.Name);
				cooldownLabel.MasterSetValue($"Cooldown: {spellEntry.Spell.CooldownTime}");
				buttonLabel.MasterSetValue("Learn");
			}
			else
			{
				spellLabel.MasterSetValue($"{spellEntry.Name} {readerSpell.CurrentTier + 1}");
				cooldownLabel.MasterSetValue("Cooldown: " +
					(readerSpell.CooldownTime - (readerSpell.CooldownTime * spellEntry.Spell.CooldownModifier)).ToString("G2"));
				buttonLabel.MasterSetValue("Upgrade");
			}

			noteLabel.MasterSetValue(
					$"{(spellEntry.RequiresWizardGarb ? "Requires wizard garb" : "Can be cast without wizard garb")} {spellEntry.Note}");
		}

		private void SetArtifactValues(SpellBookArtifact artifactEntry)
		{
			spellLabel.MasterSetValue(artifactEntry.Name);
			cooldownLabel.MasterSetValue(default);
			noteLabel.MasterSetValue(artifactEntry.Note);
			buttonLabel.MasterSetValue("Summon");
		}

		private void SetRitualValues(SpellBookRitual ritualEntry)
		{
			spellLabel.MasterSetValue(ritualEntry.Name);
			cooldownLabel.MasterSetValue(default);
			noteLabel.MasterSetValue(ritualEntry.Note);
			buttonLabel.MasterSetValue("Cast");
		}

		public void Activate()
		{
			bookGUI.ActivateEntry(entry);
		}
	}
}
