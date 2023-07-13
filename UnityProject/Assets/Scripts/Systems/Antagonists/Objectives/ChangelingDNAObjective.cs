using Antagonists;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/GetDNA")]
	public class ChangelingDNAObjective : Objective
	{
		[SerializeField] private int dnaNeedCount = 7;

		protected override bool CheckCompletion()
		{
			return Owner.Body.GetComponent<ChangelingMain>().ExtractedDNA >= dnaNeedCount;
		}

		protected override void Setup()
		{

		}
	}
}