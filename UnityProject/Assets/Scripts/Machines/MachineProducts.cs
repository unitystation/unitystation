using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MachineProducts", menuName = "ScriptableObjects/Machines/MachineProducts")]
public class MachineProducts : ScriptableObject
{
	public MachineProductCategory[] productCategoryList;
}

[System.Serializable]
public class MachineProductCategory
{
	public string categoryName;

	public MachineProduct[] products;
}

[System.Serializable]
public class MachineProduct
{
	public string name;
	public GameObject product;
}