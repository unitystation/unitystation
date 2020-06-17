using System;
using System.Collections.Generic;
using UnityEngine;

	[CreateAssetMenu(fileName = "UplinkItemList", menuName = "ScriptableObjects/PDA/UplinkItemList")]
	public class UplinkCategoryList : SingletonScriptableObject<UplinkCategoryList>
	{
		[SerializeField] [Tooltip("A list of Item categories.")]
		private List<UplinkCatagories> itemCategoryList = new List<UplinkCatagories>();

		public List<UplinkCatagories> ItemCategoryList => itemCategoryList;
	}

	[Serializable]
	public class UplinkCatagories
	{
		[SerializeField] [Tooltip("The name of the category for each uplink Item")]
		private string categoryName;

		[SerializeField] [Tooltip("The list of products in the category")]
		private List<UplinkItems> itemList = new List<UplinkItems>();

		public string CategoryName => categoryName;

		public List<UplinkItems> ItemList => itemList;
	}

	[Serializable]
	public class UplinkItems
	{
		[SerializeField] [Tooltip("Item TC cost")]
		private int cost;

		[SerializeField] [Tooltip("The prefab for the item")]
		private GameObject item;

		[SerializeField] [Tooltip("Item Name")]
		private string name;

		public string Name => name;

		public GameObject Item => item;

		public int Cost => cost;
	}
