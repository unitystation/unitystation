using Antagonists;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/GetDNA")]
	public class ChangelingDNAObjective : Objective
	{
		private ChangelingMain changeling;
		[SerializeField] private int dnaNeedCount = 7;

		protected override bool CheckCompletion()
		{
			return changeling.ExtractedDNA >= dnaNeedCount;
		}

		protected override void Setup()
		{
			changeling = Owner.Body.GetComponent<ChangelingMain>();
		}
	}
}