using UnityEngine;
using UI.Core.NetUI;
using ScriptableObjects.Items.SpellBook;

namespace UI.SpellBook
{
	public class GUI_SpellBookCategoryEntry : DynamicEntry
	{
		[SerializeField]
		private NetLabel nameLabel = default;
		[SerializeField]
		private NetLabel descriptionLabel = default;

		private GUI_SpellBook spellBook;
		private SpellBookCategory category;

		public void SetValues(GUI_SpellBook spellBook, SpellBookCategory category)
		{
			this.spellBook = spellBook;
			this.category = category;

			nameLabel.SetValueServer(category.Name);
			descriptionLabel.SetValueServer(category.Description);
		}

		public void SelectCategory()
		{
			spellBook.SelectCategory(category);
		}
	}
}
