using System;
using UnityEngine;
using ScriptableObjects.Systems.Spells;

namespace ScriptableObjects.Items.SpellBook
{
	/// <summary>
	/// A spell-type entry for a wizard's Book of Spells.
	/// </summary>
	[CreateAssetMenu(fileName = "SpellBookSpell", menuName = "ScriptableObjects/Items/SpellBook/Spell")]
	[Serializable]
	public sealed class SpellBookSpell : SpellBookEntry
	{
		[SerializeField]
		private int cooldown = default;
		[SerializeField]
		private string incantation = default;
		[SerializeField]
		private SpellData spell = default;
		
		public int Cooldown => cooldown;
		public string Incantation => incantation;
		public SpellData Spell => spell;
	}
}
