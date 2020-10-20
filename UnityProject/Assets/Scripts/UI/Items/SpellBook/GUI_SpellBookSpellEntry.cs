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

			costLabel.SetValueServer($"Cost: {entry.Cost}");
			descriptionLabel.SetValueServer(entry.Description);

			if (entry is SpellBookSpell spellEntry)
			{
				spellLabel.SetValueServer(spellEntry.Name);
				cooldownLabel.SetValueServer($"Cooldown: {spellEntry.Cooldown}");
				noteLabel.SetValueServer(
						$"{(spellEntry.RequiresWizardGarb ? "Requires wizard garb" : "Can be cast without wizard garb")} {spellEntry.Note}");
				buttonLabel.SetValueServer("Learn");
			}
			else if (entry is SpellBookArtifact artifactEntry)
			{
				spellLabel.SetValueServer(artifactEntry.Name);
				cooldownLabel.SetValueServer("");
				noteLabel.SetValueServer(artifactEntry.Note);
				buttonLabel.SetValueServer("Summon");
			}
		}

		public void Activate()
		{
			bookGUI.ActivateEntry(entry);
		}
	}
}
