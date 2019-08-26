using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ContainerData", menuName = "ScriptableObjects_BackpackData", order = 1)]
public class ContainerData : ScriptableObject
{
	public GameObject PrefabVariant;

	public EquippedData Sprites;	public StorageObjectData StorageData;
	public ItemAttributesData ItemAttributes;
}
