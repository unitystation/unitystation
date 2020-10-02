using UnityEngine;
using ScriptableObjects.Items.SpellBook;

namespace UI.SpellBook
{
	public class GUI_SpellBookSpellEntry : DynamicEntry
	{
		[SerializeField]
		private NetLabel spellLabel = default;
		[SerializeField]
		private NetLabel costLabel = default;
		[SerializeField]
		private NetLabel cooldownLabel = default;
		[SerializeField]
		private NetLabel descriptionLabel = default;
		[SerializeField]
		private NetLabel noteLabel = default;
		[SerializeField]
		private NetLabel buttonLabel = default;

		private GUI_SpellBook bookGUI;
		private SpellBookEntry entry;

		public void SetValues(GUI_SpellBook bookGUI, SpellBookEntry entry)
		{
			this.bookGUI = bookGUI;
			this.entry = entry;

			spellLabel.SetValueServer(entry.Name);
			costLabel.SetValueServer($"Cost: {entry.Cost}");
			descriptionLabel.SetValueServer(entry.Description);
			noteLabel.SetValueServer(entry.Note);

			if (entry is SpellBookSpell spellEntry)
			{
				cooldownLabel.SetValueServer($"Cooldown: {spellEntry.Cooldown}");
				buttonLabel.SetValueServer("Learn");
			}
			else if (entry is SpellBookArtifact)
			{
				cooldownLabel.SetValueServer("");
				buttonLabel.SetValueServer("Summon");
			}
		}

		public void Activate()
		{
			bookGUI.ActivateEntry(entry);
		}
	}
}
