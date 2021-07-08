using System.Collections;
using UnityEngine;
using NaughtyAttributes;

namespace ScriptableObjects.Systems.Spells
{
	[CreateAssetMenu(fileName = "MyWizardSpell", menuName = "ScriptableObjects/Systems/Spells/WizardSpellData")]
	public class WizardSpellData : SpellData
	{
		[Tooltip("Whether the spell requires wizard garb.")]
		[SerializeField, BoxGroup("Wizardry")]
		private bool requireWizardGarb = default;

		[Tooltip("The maximum level this spell can be upgraded to.")]
		[SerializeField, BoxGroup("Upgradeability"), Range(1, 10)]
		private int tierCount = 5;

		[Tooltip("What percentage each tier upgrade should take off the previous tier's cooldown time (non-linear).")]
		[SerializeField, BoxGroup("Upgradeability"), ShowIf(nameof(IsUpgradeable)), Range(0, 100)]
		private float cooldownModifierPC = 20;

		public bool RequiresWizardGarb => requireWizardGarb;
		public int TierCount => tierCount;
		public bool IsUpgradeable => tierCount > 1;
		public float CooldownModifier => cooldownModifierPC / 100;
	}
}
