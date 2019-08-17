using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class clothing
{
	public Dictionary<ClothingVariantType, int> VariantStore = new Dictionary<ClothingVariantType, int>();
	public List<int> VariantList;
	public SpriteDataForSH SpriteInfo;
}

public enum ClothingVariantType { 
	Default, 
	Tucked,
	Skirt
}