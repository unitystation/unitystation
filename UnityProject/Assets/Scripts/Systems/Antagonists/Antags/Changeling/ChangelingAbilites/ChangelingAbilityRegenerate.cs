using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Abilities/Regenerate")]
	public class ChangelingAbilityRegenerate : ChangelingData
	{
		public override bool PerfomAbility(ChangelingMain changeling, dynamic objToPerfom)
		{
			return true;
		}
	}
}