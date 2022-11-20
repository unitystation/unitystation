using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using NaughtyAttributes;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "ExplosiveBounty", menuName = "ScriptableObjects/Systems/Research/ExplosiveBounty")]
	public class ExplosiveBounty : ScriptableObject
	{
		[field: SerializeField] public BountyProperty RequiredYield { get; private set; } = new BountyProperty();

		[field: SerializeField] public List<ReactionBountyEntry> RequiredReactions { get; private set; } = new List<ReactionBountyEntry>();

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
	}

	[System.Serializable]
	public class ReactionBountyEntry : BountyProperty
	{
		[field: SerializeField] public Reaction RequiredReaction { get; private set; }
	}

	[System.Serializable]
	public class ReagentBountyEntry : BountyProperty
	{
		[field: SerializeField] public Reagent RequiredReagent { get; private set; }
	}
}
