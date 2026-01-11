using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using UnityEngine;

namespace Chemistry.Effects
{

	[Serializable]
	[CreateAssetMenu(fileName = "RemoveAdditionalReactionsSlime", menuName = "ScriptableObjects/Chemistry/Effect/RemoveAdditionalReactionsSlime")]
	public class RemoveAdditionalReactionsSlime : Chemistry.Effect
	{
		public List<Reaction> ReactionsToRemove = new List<Reaction>();



		public override void Apply(MonoBehaviour sender,ReagentMix ReagentMix,Vector3 WorldPosition, float amount)
		{
			if (sender == null) return;

			var core = sender.GetComponent<SlimeCore>();

			if (core.Enhanced )
			{
				if (core.EnhancedUsedUp)
				{
					var ReagentContainer = sender.GetComponent<ReagentContainer>();

					foreach (var ReactionToRemove in ReactionsToRemove)
					{
						ReagentContainer.AdditionalReactions.Remove(ReactionToRemove);
					}
				}
				else
				{
					core.EnhancedUsedUp = true;
				}

			}
			else
			{
				var ReagentContainer = sender.GetComponent<ReagentContainer>();

				foreach (var ReactionToRemove in ReactionsToRemove)
				{
					ReagentContainer.AdditionalReactions.Remove(ReactionToRemove);
				}
			}
		}
	}
}
