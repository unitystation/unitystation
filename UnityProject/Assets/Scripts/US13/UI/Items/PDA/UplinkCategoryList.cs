using System;
using System.Collections.Generic;
using UnityEngine;
using US13.ScriptableObjects;
using US13.Systems.Occupations;

namespace US13.UI.Items.PDA
{
	[CreateAssetMenu(fileName = "UplinkItemList", menuName = "ScriptableObjects/PDA/UplinkItemList")]
	public class UplinkCategoryList : SingletonScriptableObject<UplinkCategoryList>
	{
		[SerializeField] [Tooltip("A list of Item categories.")]
		private List<UplinkCategory> itemCategoryList = new List<UplinkCategory>();

		public List<UplinkCategory> ItemCategoryList => itemCategoryList;
	}

	[Serializable]
	public class UplinkCategory
	{
		[SerializeField] [Tooltip("The name of the category for each uplink Item")]
		private string categoryName = "";

		[SerializeField] [Tooltip("The list of products in the category")]
		private List<UplinkItem> itemList = new List<UplinkItem>();

		public string CategoryName => categoryName;

		public List<UplinkItem> ItemList => itemList;

		public override string ToString()
		{
			return $"UplinkCategory: {CategoryName} ({ItemList.Count} items)";
		}
	}

	[Serializable]
	public class UplinkItem
	{
		[SerializeField] [Tooltip("Item TC cost")]
		private int cost = 1;

		[SerializeField] [Tooltip("The prefab for the item")]
		private GameObject item = null;

		[SerializeField] [Tooltip("Item Name")]
		private string name = "";

		[SerializeField] [Tooltip("Determins if this item is displayed to nuke ops")]
		private bool isNukeOps = false;

		[SerializeField, Tooltip("Restricted to certain job types")]
		private JobType jobType = JobType.NULL;

		[SerializeField, Tooltip("Exclude from being randomly picked")]
		private bool excludedRandomPick = false;


		public bool ExcludedRandomPick => excludedRandomPick;

		public JobType JobType => jobType;

		public bool IsNukeOps => isNukeOps;

		public string Name => name;

		public GameObject Item => item;

		public int Cost => cost;
	}
}