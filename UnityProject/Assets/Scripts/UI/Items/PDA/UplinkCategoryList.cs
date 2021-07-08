using System;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

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
		public bool IsNukeOps => isNukeOps;

		public string Name => name;

		public GameObject Item => item;

		public int Cost => cost;
	}
