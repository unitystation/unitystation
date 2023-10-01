using System;
using System.Collections;
using System.Collections.Generic;
using Initialisation;
using Systems.Research.Data;
using UnityEngine;
using Util;
using Random = UnityEngine.Random;

public class ItemResearchPotential : MonoBehaviour
{


	public bool IsTooPure = false;

	public PrefabTracker PrefabTracker;

	private static Dictionary<string, ItemResearchPotentialData> ItemResearchRandomisedData = new Dictionary<string, ItemResearchPotentialData>();
	private static int RoundID = 0;


	[SerializeField,Range(0,100)]
	private int BasePurity = 15;


  	[NonSerialized] public int CurrentPurity = 0;
	public List<TechnologyAndBeams> TechWebDesigns;


	public void Awake()
	{
		int Round = GameManager.RoundID;
		if (Round != RoundID)
		{
			ItemResearchRandomisedData.Clear();
			RoundID = Round;
		}

		LoadManager.RegisterAction(InitialiseData);



	}


	public void InitialiseData()
	{
		if (ItemResearchRandomisedData.ContainsKey(PrefabTracker.ForeverID) == false)
		{
			ItemResearchRandomisedData[PrefabTracker.ForeverID] = GenerateItemResearchPotentialData();
		}

		ApplyItemResearchPotentialData(ItemResearchRandomisedData[PrefabTracker.ForeverID]);
	}

	public ItemResearchPotentialData GenerateItemResearchPotentialData()
	{
		int Techs = 1;

		var toReturn = new ItemResearchPotentialData();
		toReturn.TechWebDesigns = new List<TechnologyAndBeams>();
		System.Random random = new System.Random();
		if (random.NextDouble() < 0.15)
		{
			int min = 20;
			int max = 35;
			toReturn.AddedPurity = Random.Range(min, max + 1);
		}
		else
		{
			int min = -25;
			int max = 25;
			toReturn.AddedPurity = Random.Range(min, max + 1);
		}


		if (BasePurity + toReturn.AddedPurity > 25)
		{
			var rng = Random.Range(0, 1000);
			if (rng > 950) //5% now
			{
				toReturn.AddedPurity += 100;
				toReturn.IsTooPure = true;
			}
		}

		if (BasePurity + toReturn.AddedPurity > 50)
		{
			bool randomBool = random.Next(2) == 0;
			if (randomBool)
			{
				Techs++;
			}

			if (BasePurity + toReturn.AddedPurity > 75)
			{
				randomBool = random.Next(2) == 0;
				if (randomBool)
				{
					Techs++;
				}
			}
		}
		var InNumberOfBeams = (int)Math.Round((BasePurity + toReturn.AddedPurity + Random.Range(-10,  30)) / 20f); //100 = 5, 0 = 0

		//DEBUG
		//Techs = 2;
		//InNumberOfBeams = 2;
		for (int i = 0; i < Techs; i++)
		{
			var data = new TechnologyAndBeams();
			data.Beams = new List<int>();

			for (int j = 0; j < InNumberOfBeams; j++)
			{
				data.Beams.Add(Random.Range(35, 325 + 1));
			}
			toReturn.TechWebDesigns.Add(data);
		}

		return toReturn;
	}

	public void ApplyItemResearchPotentialData(ItemResearchPotentialData ItemResearchPotentialData)
	{
		IsTooPure = ItemResearchPotentialData.IsTooPure;
		CurrentPurity = BasePurity + ItemResearchPotentialData.AddedPurity;
		TechWebDesigns = ItemResearchPotentialData.TechWebDesigns;
	}
}


public struct ItemResearchPotentialData
{
	public bool IsTooPure;
	public int AddedPurity;
	public List<TechnologyAndBeams> TechWebDesigns;
}
public class TechnologyAndBeams
{
	public Technology Technology;
	public List<int> Beams;
	public Color Colour = Color.white;

}