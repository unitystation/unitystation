using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using NaughtyAttributes;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "ExplosiveBounty", menuName = "ScriptableObjects/Systems/Research/ExplosiveBounty")]
	public class ExplosiveBounty : ScriptableObject
	{
		[field: SerializeField] public string BountyName { get; private set; } = null;

		[field: SerializeField] public BountyProperty RequiredYield { get; private set; } = new BountyProperty();

		[field: SerializeField] public List<EffectBountyEntry> RequiredEffects { get; private set; } = new List<EffectBountyEntry>();

		[field: SerializeField] public List<ReagentBountyEntry> RequiredReagents { get; private set; } = new List<ReagentBountyEntry>();
	}

	[System.Serializable]
	public class BountyProperty
	{
		[field: SerializeField, Tooltip("If true, required amount will be randomised between the supplied range")]
		public bool RandomiseRequirement { get; private set; }

		[field: SerializeField, AllowNesting, HideIf(nameof(RandomiseRequirement))]
		public int RequiredAmount { get; set; }

		[field: SerializeField, AllowNesting, ShowIf(nameof(RandomiseRequirement))]
		public int MinAmount { get; private set; }

		[field: SerializeField, AllowNesting, ShowIf(nameof(RandomiseRequirement))]
		public int MaxAmount { get; private set; }

		[field: SerializeField, Tooltip("When randomising requirements, round to the nearest minimum increment."), AllowNesting, ShowIf(nameof(RandomiseRequirement))]
		public int MinimumIncrement { get; private set; } = 1;
	}

	[System.Serializable]
	public class EffectBountyEntry : BountyProperty
	{
		[field: SerializeField] public Chemistry.Effect RequiredEffect { get; private set; }
	}

	[System.Serializable]
	public class ReagentBountyEntry : BountyProperty
	{
		[field: SerializeField] public Reagent RequiredReagent { get; private set; }
	}
}
