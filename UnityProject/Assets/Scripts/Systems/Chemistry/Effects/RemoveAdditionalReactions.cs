using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using UnityEngine;

namespace Chemistry.Effects
{

	[Serializable]
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Effect/RemoveAdditionalReactions")]
	public class RemoveAdditionalReactions : Chemistry.Effect
	{
		public List<Reaction> ReactionsToRemove = new List<Reaction>();



		public override void Apply(MonoBehaviour sender, float amount)
		{
			var ReagentContainer = sender.GetComponent<ReagentContainer>();

			foreach (var ReactionToRemove in ReactionsToRemove)
			{
				ReagentContainer.AdditionalReactions.Remove(ReactionToRemove);
			}
		}
	}
}
