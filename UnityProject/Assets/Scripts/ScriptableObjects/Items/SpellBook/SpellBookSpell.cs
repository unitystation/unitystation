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
		private WizardSpellData spell = default;

		public string Name => spell.Name;
		public float Cooldown => spell.CooldownTime;
		public string Incantation => spell.InvocationMessage;
		public bool RequiresWizardGarb => spell.RequiresWizardGarb;
		public WizardSpellData Spell => spell;
	}
}
