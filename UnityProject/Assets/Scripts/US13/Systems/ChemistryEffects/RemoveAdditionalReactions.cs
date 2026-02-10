using System;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.ChemistryComponents;

namespace US13.Systems.ChemistryEffects
{
	[Serializable]
	[CreateAssetMenu(fileName = "RemoveAdditionalReactions", menuName = "ScriptableObjects/Chemistry/Effect/RemoveAdditionalReactions")]
	public class RemoveAdditionalReactions : Chemistry.Effect
	{
		public List<Reaction> ReactionsToRemove = new List<Reaction>();

		public override void Apply(MonoBehaviour sender,ReagentMix ReagentMix,Vector3 WorldPosition, float amount)
		{
			if (sender == null) return;
			var ReagentContainer = sender.GetComponent<ReagentContainer>();

			foreach (var ReactionToRemove in ReactionsToRemove)
			{
				ReagentContainer.AdditionalReactions.Remove(ReactionToRemove);
			}
		}
	}
}