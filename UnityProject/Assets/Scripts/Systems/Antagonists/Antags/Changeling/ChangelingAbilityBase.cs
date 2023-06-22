using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Abilities/Base")]
public class ChangelingAbilityBase : ScriptableObject
{
	[Header("Main")]
	public string abilityName;
	[TextArea(10, 20)]
	public string abilityDescription;

	[Header("Variables")]
	// ep - evolutian point
	public int abilityEPCost;
	public int abilityChemCost;

	public void PerfomAbility(ChangelingMain changeling)
	{

	}

	public (int, int) GetAbilityPrice()
	{
		return (abilityEPCost, abilityChemCost);
	}
}
