using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	private string categoryName;

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
	private string name;

	public string Name { get => name; }

	[SerializeField]
	[Tooltip("Product Prefab")]
	private GameObject product;

	public GameObject Product { get => product; }

	[SerializeField]
	[Tooltip("Product material cost")]
	private List<MachineProductMaterialPrice> materialPrice = new List<MachineProductMaterialPrice>();

	public List<MachineProductMaterialPrice> MaterialPrice { get => materialPrice; }

	[SerializeField]
	[Tooltip("Base time it takes to create the product")]
	private float productionTime = 10;

	public float ProductionTime { get => productionTime; }
}

//This is used to define material price of materials for a certain product. If
//items get a component holding the value, this should be refactored.
[System.Serializable]
public class MachineProductMaterialPrice
{
	[SerializeField]
	private int Iron;

	[SerializeField]
	private int Glass;

	[SerializeField]
	private int Silver;

	[SerializeField]
	private int Gold;

	[SerializeField]
	private int Plasma;

	[SerializeField]
	private int Uranium;

	[SerializeField]
	private int Titanium;

	[SerializeField]
	private int Diamond;

	[SerializeField]
	private int BluespaceCrystal;

	[SerializeField]
	private int Plastic;

	[SerializeField]
	public static GameObject materials;

	[SerializeField]
	[Tooltip("The material type, materials have an item trait according to their types.")]
	private ItemTrait material;

	public ItemTrait Material { get => material; }

	[SerializeField]
	[Tooltip("The amount of materials the product costs.")]
	private int amount;

	public int Amount { get => amount; }
}