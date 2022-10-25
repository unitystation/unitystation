using UnityEngine;
using UI.Core.NetUI;
using ScriptableObjects.Items.SpellBook;

namespace UI.SpellBook
{
	public class GUI_SpellBookCategoryEntry : DynamicEntry
	{
		[SerializeField]
		private NetText_label nameLabel = default;
		[SerializeField]
		private NetText_label descriptionLabel = default;

		private GUI_SpellBook spellBook;
		private SpellBookCategory category;

		public void SetValues(GUI_SpellBook spellBook, SpellBookCategory category)
		{
			this.spellBook = spellBook;
			this.category = category;

			nameLabel.MasterSetValue(category.Name);
			descriptionLabel.MasterSetValue(category.Description);
		}

		public void SelectCategory()
		{
			spellBook.SelectCategory(category);
		}
	}
}
