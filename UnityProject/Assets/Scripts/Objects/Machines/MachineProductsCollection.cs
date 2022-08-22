using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Objects.Machines
{
	[CreateAssetMenu(fileName = "MachineProductsCollection", menuName = "ScriptableObjects/Machines/MachineProductsCollection")]
	public class MachineProductsCollection : ScriptableObject
	{
		[SerializeField]
		[Tooltip("A list of product categories.")]
		private List<MachineProductList> productCategoryList = new List<MachineProductList>();

		public List<MachineProductList> ProductCategoryList { get => productCategoryList; }
	}

	[System.Serializable]
	public class MachineProductList
	{
		[SerializeField]
		[Tooltip("Category name for a list of products.")]
		private string categoryName = null;

		public string CategoryName { get => categoryName; }

		[SerializeField]
		[Tooltip("The list of products in this category")]
		private List<MachineProduct> products = new List<MachineProduct>();

		public List<MachineProduct> Products { get => products; }
	}

	[System.Serializable]
	public class MachineProduct
	{
		[SerializeField]
		[Tooltip("Product name.")]
		private string name = null;

		public string Name { get => name; }

		[SerializeField]
		[Tooltip("Product Prefab")]
		private GameObject product = null;

		public GameObject Product { get => product; }

		[SerializeField]
		[Tooltip("Product material cost")]
		public SerializableDictionary<MaterialSheet, int> materialToAmounts;

		[SerializeField]
		[Tooltip("Base time it takes to create the product")]
		private float productionTime = 10;

		public float ProductionTime { get => productionTime; }
	}
}
