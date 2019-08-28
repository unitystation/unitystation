using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeadsetData", menuName = "ScriptableObjects/HeadsetData", order = 2)]
public class HeadsetData : ScriptableObject
{
	public GameObject PrefabVariant;
	public EquippedData Sprites; 
	public ItemAttributesData ItemAttributes;
	public HeadsetKyes Key;
}


[System.Serializable]
public class HeadsetKyes
{
	public EncryptionKeyType EncryptionKey;
}