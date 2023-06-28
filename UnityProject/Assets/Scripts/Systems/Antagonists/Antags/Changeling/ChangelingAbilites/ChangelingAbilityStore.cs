using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Abilities/Store")]
	public class ChangelingAbilityStore : ChangelingData
	{
		public override bool PerfomAbility(ChangelingMain changeling, dynamic objToPerfom)
		{
			return true;
		}
	}
}