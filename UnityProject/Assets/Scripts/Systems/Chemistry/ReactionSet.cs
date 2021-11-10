using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace Systems.Chemistry
{
	[CreateAssetMenu(fileName = "reactionSet", menuName = "ScriptableObjects/Chemistry/ReactionSet")]
	public class ReactionSet : ScriptableObject
	{
		public ReactionSet[] parents = new ReactionSet[0];
		public Reaction[] reactions = new Reaction[0];

		public virtual bool Apply(MonoBehaviour sender, ReagentMix reagentMix, List<Reaction> AdditionalReactions = null)
		{
			bool changing;
			var changed = false;
			do
			{
				changing = false;
				foreach (var parent in parents)
				{
					if (parent.Apply(sender, reagentMix))
					{
						changing = true;
						changed = true;
					}
				}

				foreach (var reaction in reactions)
				{
					if (reaction.Apply(sender, reagentMix))
					{
						changing = true;
						changed = true;
					}
				}

				if (AdditionalReactions != null)
				{
					foreach (var reaction in AdditionalReactions)
					{
						if (reaction.Apply(sender, reagentMix))
						{
							changing = true;
							changed = true;
						}
					}
				}
			} while (changing);

			return changed;
		}
	}
}