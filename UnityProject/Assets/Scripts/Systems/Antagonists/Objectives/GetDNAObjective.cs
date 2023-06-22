using Antagonists;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetDNAObjective : Objective
{
	private ChangelingMain changeling;
	[SerializeField] private int dnaNeedCount = 7;

	protected override bool CheckCompletion()
	{
		return changeling.GetDNA() >= dnaNeedCount;
	}

	protected override void Setup()
	{
		changeling = Owner.Body.GetComponent<ChangelingMain>();
	}
}
