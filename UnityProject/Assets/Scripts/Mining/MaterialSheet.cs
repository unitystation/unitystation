using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialsInMachineStorage", menuName = "ScriptableObjects/Mining/MaterialSheet")]
public class MaterialSheet : ScriptableObject
{
	public GameObject OrePrefab;
	public string displayName;
	public GameObject RefinedPrefab;
	public ItemTrait materialTrait;
}