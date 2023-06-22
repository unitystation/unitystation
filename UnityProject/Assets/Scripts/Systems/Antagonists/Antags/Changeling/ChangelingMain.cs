using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangelingMain : MonoBehaviour
{
	[Header("Changeling abilites")]
	[SerializeField] private float chim = 0;
	[SerializeField] private float chimMax = 100;
	[SerializeField] private float chimAdd = 1;
	[SerializeField] private float chimAddTime = 5;
	[SerializeField] private int extractedDNA = 1;
	[SerializeField] private List<string> changelingDNAs = new List<string>();
	[SerializeField] private int maxExtractedDNA = 7;
	// ep - evolutian point
	[SerializeField] private int epPoints = 10;
	[SerializeField] List<ChangelingAbilityBase> abilitiesToBuy = new List<ChangelingAbilityBase>();
	[SerializeField] List<ChangelingAbilityBase> abilitiesNow = new List<ChangelingAbilityBase>();

	public void FellInStasis()
	{

	}

	public void WakeUpFromStasis()
	{

	}

	public void BuyAbility()
	{
		
	}

	public int abilityNomber = 0;

	public void UseAbility()
	{
		abilitiesNow[abilityNomber].PerfomAbility(this);
	}

	void Start()
	{
		//print("HI BRUH");
		UpdateManager.Add(ChimAddUpdate, chimAddTime);
		chim = 0;
	}

	void ChimAddUpdate()
	{
		chim += chimAdd;
	}

	public int GetDNA()
	{
		return extractedDNA;
	}

	public void SetAbilities(List<ChangelingAbilityBase> abilitiesToBuy, List<ChangelingAbilityBase> abilitiesBasic)
	{
		abilitiesNow.Clear();
		abilitiesNow.AddRange(abilitiesBasic);

		abilitiesToBuy.Clear();
		abilitiesToBuy.AddRange(abilitiesToBuy);
	}
}
