using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Magical;
using ScriptableObjects.Systems.Spells;
using ScriptableObjects.Items.SpellBook;
using Systems.Spells;

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
		private SpellBookCategory currentCategory;

		public int Points => spellBook.Points;

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

			currentCategory = category;
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
				ServerCloseTabFor(spellBook.GetLastReader()); // We close tab so that the player is aware of the dropping pod.
			}
			else if (entry is SpellBookRitual ritualEntry)
			{
				spellBook.CastRitual(ritualEntry);
			}

			RefreshCategory();
			UpdatePoints();
		}

		private void RefreshCategory()
		{
			SelectCategory(currentCategory);
		}

		private void UpdatePoints()
		{
			pointsCounter.SetValueServer($"Points: {Points}");
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
			List<int> spellsToAdd = new List<int>();
			for (int i = 0; i < category.EntryList.Count; i++)
			{
				if (category.EntryList[i] is SpellBookSpell spellEntry)
				{
					if (spellBook.GetReaderSpellLevel(spellEntry.Spell) >= spellEntry.Spell.TierCount) continue;
					if (spellBook.ReaderSpellsConflictWith(spellEntry)) continue;
				}

				spellsToAdd.Add(i);
			}

			// Double for loop until we can add items to the dynamic list individually.

			entryList.AddItems(spellsToAdd.Count);
			for (int i = 0; i < spellsToAdd.Count; i++)
			{
				entryList.Entries[i].GetComponent<GUI_SpellBookEntry>().SetValues(this, category.EntryList[spellsToAdd[i]]);
			}
		}

		public Spell GetReaderSpellInstance(WizardSpellData spell)
		{
			return spellBook.GetReaderSpellInstance(spell);
		}
	}
}
