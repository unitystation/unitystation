using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemAttributesData
{
	public string itemName;
	public string itemDescription;
	public ItemType itemType = ItemType.None;
	public ItemSize size;
	public SpriteType spriteType;
	public bool CanConnectToTank;
	public bool IsEVACapable;

	[Tooltip("Damage when we click someone with harm intent")]
	[Range(0, 100)]
	public float hitDamage = 0;

	public DamageType damageType = DamageType.Brute;

	[Tooltip("How painful it is when someone throws it at you")]
	[Range(0, 100)]
	public float throwDamage = 0;

	[Tooltip("How many tiles to move per 0.1s when being thrown")]
	public float throwSpeed = 2;

	[Tooltip("Max throw distance")]
	public float throwRange = 7;

	[Tooltip("Sound to be played when we click someone with harm intent")]
	public string hitSound = "GenericHit";

	public List<string> attackVerb = new List<string>();
}
