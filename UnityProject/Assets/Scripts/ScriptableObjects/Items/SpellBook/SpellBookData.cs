using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace ScriptableObjects.Items.SpellBook
{
	/// <summary>
	/// Contains all the data needed to populate a wizard's Book of Spells.
	/// </summary>
	[CreateAssetMenu(fileName = "SpellBookData", menuName = "ScriptableObjects/Items/SpellBook/SpellBookData")]
	[Serializable]
	public class SpellBookData : ScriptableObject
	{
		[SerializeField]
		private List<SpellBookCategory> categoryList = default;

		public List<SpellBookCategory> CategoryList => categoryList;
	}

	/// <summary>
	/// The categories by which entries for a wizard's Book of Spells is organised.
	/// </summary>
	[Serializable]
	public sealed class SpellBookCategory
	{
		[SerializeField]
		private string name = default;
		[SerializeField]
		private string description = default;
		[SerializeField, ReorderableList]
		private List<SpellBookEntry> entryList = default;

		public string Name => name;
		public string Description => description;
		public List<SpellBookEntry> EntryList => entryList;

		public override string ToString()
		{
			return $"SpellBookCategory: {Name} ({EntryList.Count} items)";
		}
	}

	/// <summary>
	/// An abstract class containing properties common to all entries in a wizard's Book of Spells.
	/// </summary>
	[Serializable]
	public abstract class SpellBookEntry : ScriptableObject
	{
		[SerializeField]
		private string description = default;
		[Tooltip("Any other info (not including 'Requires wizard garb.')")]
		[SerializeField]
		private string note = default;
		[SerializeField]
		private int cost = 2;
		[Tooltip("If this spell is purchased, these listed spells will become unavailable.")]
		[SerializeField]
		private SpellBookEntry[] conflictsWith = default;

		public string Description => description;
		public string Note => note;
		public int Cost => cost;
		public SpellBookEntry[] ConflictsWith => conflictsWith;
	}
}
