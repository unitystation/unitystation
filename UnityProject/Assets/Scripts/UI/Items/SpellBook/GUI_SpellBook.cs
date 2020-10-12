using System.Collections;
using UnityEngine;
using Items.Magical;
using ScriptableObjects.Items.SpellBook;

namespace UI.SpellBook
{
	public class GUI_SpellBook : NetTab
	{
		[SerializeField]
		private NetLabel pointsCounter = default;
		[SerializeField]
		private EmptyItemList categoryList = default;
		[SerializeField]
		private EmptyItemList entryList = default;

		private BookOfSpells spellBook;

		#region Lifecycle

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			spellBook = Provider.GetComponent<BookOfSpells>();

			UpdatePoints();
			GenerateCategories();
		}

		#endregion Lifecycle

		public void SelectCategory(SpellBookCategory category)
		{
			entryList.Clear();
			GenerateEntries(category);
		}

		public void ActivateEntry(SpellBookEntry entry)
		{
			if (entry is SpellBookSpell spellEntry)
			{
				spellBook.LearnSpell(spellEntry);
			}
			else if (entry is SpellBookArtifact artifactEntry)
			{
				spellBook.SpawnArtifacts(artifactEntry);
				CloseTab(); // We close tab so that the player is aware of the dropping pod.
			}

			UpdatePoints();
		}

		private void UpdatePoints()
		{
			pointsCounter.SetValueServer($"Points: {spellBook.Points}");
		}

		private void GenerateCategories()
		{
			var categories = spellBook.Data.CategoryList;

			categoryList.AddItems(categories.Count);
			for (int i = 0; i < categories.Count; i++)
			{
				categoryList.Entries[i].GetComponent<GUI_SpellBookCategoryEntry>().SetValues(this, categories[i]);
			}
		}

		private void GenerateEntries(SpellBookCategory category)
		{
			entryList.AddItems(category.EntryList.Count);
			for (int i = 0; i < category.EntryList.Count; i++)
			{
				entryList.Entries[i].GetComponent<GUI_SpellBookSpellEntry>().SetValues(this, category.EntryList[i]);
			}
		}
	}
}
