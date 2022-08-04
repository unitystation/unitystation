using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;

public class ChemicalGrenade : MonoBehaviour
{
	[RightClickMethod()]
	public void DoSmartFoam()
	{
		SmokeAndFoamManager.StartFoamAt(transform.position, new ReagentMix(), 50, true, true);
	}

	[RightClickMethod()]
	public void DoFoam()
	{
		SmokeAndFoamManager.StartFoamAt(transform.position, new ReagentMix(), 50, true, false);
	}
}
