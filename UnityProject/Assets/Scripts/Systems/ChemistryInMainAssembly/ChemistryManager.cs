using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chemistry;
using JetBrains.Annotations;
using ScriptableObjects;
using Shared.Managers;
using UnityEngine;

public class ChemistryManager : SingletonManager<ChemistryManager>
{
	public ReactionSet ReactionSet;

	private static bool generatedReferences = false;

	public void Awake()
	{
		new Task(ChemistryReagentsSO.Instance.GenerateReagentReactionReferences).Start();
	}

	public static void ReagentsChanged(
		[CanBeNull] MonoBehaviour sender,
		ReagentMix CurrentReagentMix,
		HashSet<Chemistry.Reaction> ContainedAdditionalReactions,
		HashSet<Chemistry.Reaction> possibleReactions,
		[CanBeNull] ReactionSet ReactionSet,
		Vector3 HappeningAtWorld,
		bool ReactionSounds = true,
		bool applyChange = true,
		bool cacheEffects = false)
	{
		if (ReactionSet == null)
		{
			ReactionSet = ChemistryManager.Instance.ReactionSet;
		}


		possibleReactions.Clear();
		foreach (var reagents in CurrentReagentMix.reagents.m_dict)
		{
			var reactions = reagents.Key.RelatedReactions;
			int reactionsCount = reactions.Length;
			for (int i = 0; i < reactionsCount; i++)
			{
				var reaction = reactions[i];
				if (ReactionSet != null && ReactionSet.ContainedReactionss.Contains(reaction))
				{
					possibleReactions.Add(reaction);
				}
				else if (ContainedAdditionalReactions is {Count: > 0} &&
				         ContainedAdditionalReactions.Contains(reaction))
				{
					possibleReactions.Add(reaction);
				}
			}
		}

		if (cacheEffects)
		{
			CurrentReagentMix.CacheReactionEffects(
				ReactionSet.ApplyWithoutEffects(CurrentReagentMix, possibleReactions));
			return;
		}

		if (applyChange == true)
		{
			var Changed = ReactionSet.Apply(sender, HappeningAtWorld, CurrentReagentMix, possibleReactions);

			if (Changed && ReactionSounds)
			{
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Bubbles, HappeningAtWorld);
			}
		}
	}
}