using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using NaughtyAttributes;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "ExplosiveBounty", menuName = "ScriptableObjects/Systems/Research/ExplosiveBounty")]
	public class ExplosiveBounty : ScriptableObject
	{
		public BountyProperty RequiredYield = new BountyProperty();

		public List<ReactionBountyEntry> RequiredReactions = new List<ReactionBountyEntry>();

		public List<ReagentBountyEntry> RequiredReagents = new List<ReagentBountyEntry>();
	}

	[System.Serializable]
	public class BountyProperty
	{
		[Tooltip("If true, required amount will be randomised between the supplied range")]
		public bool RandomiseRequirement;

		[AllowNesting, HideIf(nameof(RandomiseRequirement))]
		public int requiredAmount;

		[AllowNesting, ShowIf(nameof(RandomiseRequirement))]
		public int MinAmount;
		[AllowNesting, ShowIf(nameof(RandomiseRequirement))]
		public int MaxAmount;
	}

	[System.Serializable]
	public class ReactionBountyEntry : BountyProperty
	{
		public Reaction requiredReaction;
	}

	[System.Serializable]
	public class ReagentBountyEntry : BountyProperty
	{
		public Reagent requiredReagent;
	}
}
