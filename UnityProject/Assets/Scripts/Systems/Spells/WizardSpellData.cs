using System.Collections;
using UnityEngine;

namespace ScriptableObjects.Systems.Spells
{
	[CreateAssetMenu(fileName = "MyWizardSpell", menuName = "ScriptableObjects/Systems/Spells/WizardSpellData")]
	public class WizardSpellData : SpellData
	{
		[Header("Whether the spell requires wizard garb.")]
		[SerializeField] private bool requireWizardGarb = default;

		public bool RequiresWizardGarb => requireWizardGarb;
	}
}
