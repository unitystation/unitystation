using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace Chemistry
{
	[CreateAssetMenu(fileName = "reactionSet", menuName = "ScriptableObjects/Chemistry/ReactionSet")]
	public class ReactionSet : ScriptableObject
	{
		public ReactionSet[] parents = new ReactionSet[0];
		public Reaction[] reactions = new Reaction[0];

		private HashSet<Reaction> containedReactionss;


		//Includes everything on parents to, if needs to be dynamic can change
		public HashSet<Reaction> ContainedReactionss
		{
			get
			{
				lock (reactions)
				{
					if (containedReactionss == null)
					{
						containedReactionss = new HashSet<Reaction>();
						foreach (var Reaction in reactions)
						{
							containedReactionss.Add(Reaction);
						}

						foreach (var Parent in parents)
						{
							//humm Careful there is a loop here
							containedReactionss.UnionWith(Parent.ContainedReactionss);
						}
					}

					return containedReactionss;
				}
			}
		}

		public static bool Apply(MonoBehaviour sender, ReagentMix reagentMix, HashSet<Reaction> possibleReactions)
		{
			bool changing;
			var changed = false;
			do
			{
				changing = false;
				foreach (var reaction in possibleReactions)
				{
					if (reaction.Apply(sender, reagentMix))
					{
						changing = true;
						changed = true;
					}
				}

			} while (changing);

			return changed;
		}

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