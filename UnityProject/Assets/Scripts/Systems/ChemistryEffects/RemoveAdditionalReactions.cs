using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Chemistry.Components;
using UnityEngine;

namespace Chemistry.Effects
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