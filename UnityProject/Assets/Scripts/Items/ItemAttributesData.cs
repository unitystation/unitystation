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
	public List<ItemTrait> traits = new List<ItemTrait>();

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

	public void Combine(ItemAttributesData parent)
	{
		if (string.IsNullOrEmpty(itemName))
		{
			itemName = parent.itemName;
		}

		if (string.IsNullOrEmpty(itemDescription))
		{
			itemDescription = parent.itemDescription;
		}

		if (itemType == ItemType.None)
		{
			itemType = parent.itemType;
		}

		if (size == ItemSize.None)
		{
			size = parent.size;
		}

		if (hitDamage.Equals(-1))
		{
			hitDamage = parent.hitDamage;
		}

		if (throwDamage.Equals(-1))
		{
			throwDamage = parent.throwDamage;
		}

		if (throwSpeed.Equals(-1))
		{
			throwSpeed = parent.throwSpeed;
		}

		if (throwRange.Equals(-1))
		{
			throwRange = parent.throwRange;
		}

		if (hitSound != null)
		{
			hitSound = parent.hitSound;
		}

		if (attackVerb.Count > 0)
		{
			attackVerb = parent.attackVerb;
		}
	}
}
