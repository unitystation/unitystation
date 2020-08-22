using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;

public class BZFormationReaction : Reaction
{
	public bool Satisfies(GasMix gasMix)
	{
		throw new System.NotImplementedException();
	}

	public float React(ref GasMix gasMix, GasReactions gasReaction)
	{
		var totalMoles = 0f;

		foreach (var data in gasReaction.GasReactionData)
		{
			var moles = gasMix.GetMoles(data.Key);

			if (moles > 20f)
			{
				gasMix.RemoveGas(data.Key, 20f * data.Value.ratio);
			}
			else
			{
				gasMix.RemoveGas(data.Key, moles * data.Value.ratio);

				totalMoles += moles * data.Value.ratio;
			}
		}

		gasMix.AddGas(gasReaction.GasCreated, totalMoles);

		return totalMoles;
	}
}
