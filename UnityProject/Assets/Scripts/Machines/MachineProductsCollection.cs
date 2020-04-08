using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MachineProductsCollection", menuName = "ScriptableObjects/Machines/MachineProductsCollection")]
public class MachineProductsCollection : ScriptableObject
{
	[Tooltip("A list of product categories.")]
	public MachineProductCategory[] productCategoryList;
}

[System.Serializable]
public class MachineProductCategory
{
	[Tooltip("Category name for a list of products.")]
	public string categoryName;

	[Tooltip("The list of products in this category")]
	public MachineProduct[] products;
}

[System.Serializable]
public class MachineProduct
{
	[Tooltip("Product name.")]
	public string name;

	[Tooltip("Product Prefab")]
	public GameObject product;

	[Tooltip("Product material cost")]
	public MachineProductMaterialPrice[] materialPrice;
}

//This is used to define material price of materials for a certain product. If
//items get a component holding the value, this should be refactored.
[System.Serializable]
public class MachineProductMaterialPrice
{
	[Tooltip("The material type, materials have an item trait according to their types.")]
	public ItemTrait material;

	[Tooltip("The amount of materials the product costs.")]
	public int amount;
}