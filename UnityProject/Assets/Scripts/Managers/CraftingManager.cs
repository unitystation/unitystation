﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Objects.Machines;

public class CraftingManager : MonoBehaviour
{
	private static CraftingManager craftingManager;
	[SerializeField] private CraftingDatabase meals = new CraftingDatabase();
	[SerializeField] private CraftingDatabase cuts = new CraftingDatabase();
	[SerializeField] private CraftingDatabase logs = new CraftingDatabase();
	[SerializeField] private CraftingDatabase roll = new CraftingDatabase();
	[SerializeField] private CraftingDatabase simplemeal = new CraftingDatabase();
	[SerializeField] private GrinderDatabase grind = new GrinderDatabase();
	[SerializeField] private CraftingDatabase mix = new CraftingDatabase();


	[SerializeField]
	private List<MaterialSheet> MaterialSheetList;
	public static Dictionary<ItemTrait, MaterialSheet> MaterialSheetData = new Dictionary<ItemTrait, MaterialSheet>();
	public static MaterialSilo RoundstartStationSilo;

	public static CraftingDatabase Meals => Instance.meals;
	public static CraftingDatabase Cuts => Instance.cuts;
	public static CraftingDatabase Logs => Instance.logs;
	public static CraftingDatabase Roll => Instance.roll;
	public static CraftingDatabase SimpleMeal => Instance.simplemeal;
	public static GrinderDatabase Grind => Instance.grind;
	public static CraftingDatabase Mix => Instance.mix;

	private void Awake()
	{
		MaterialSheetData.Clear();
		foreach (var material in MaterialSheetList)
		{
			MaterialSheetData.Add(material.materialTrait, material);
		}
	}

	public static CraftingManager Instance
	{
		get
		{
			if (!craftingManager)
			{
				craftingManager = FindObjectOfType<CraftingManager>();
			}

			return craftingManager;
		}
	}

	public Techweb techweb;
	public Designs designs;

	public static Designs Designs => Instance.designs;
	public static Techweb TechWeb => Instance.techweb;
}