using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialsInMachineStorage", menuName = "ScriptableObjects/Mining/MaterialSheet")]
public class MaterialSheet : ScriptableObject
{
	public int laborPoint;
	public ItemTrait oreTrait;
	public string displayName;
	public GameObject RefinedPrefab;
	public ItemTrait materialTrait;
}