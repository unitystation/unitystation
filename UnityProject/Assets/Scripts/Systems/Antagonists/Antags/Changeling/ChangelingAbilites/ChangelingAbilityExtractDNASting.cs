using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Abilities/ExtractDNASting")]
	public class ChangelingAbilityExtractDNASting : ChangelingData
	{
		public override bool PerfomAbility(ChangelingMain changeling, dynamic objToPerfom)
		{
			return true;
		}
	}
}