using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clothing : MonoBehaviour
{
	public Dictionary<ClothingVariantType, int> VariantStore = new Dictionary<ClothingVariantType, int>();
	public List<int> VariantList;
	public SpriteDataForSH SpriteInfo;

	public int ReturnState(ClothingVariantType CVT) {
		if (VariantStore.ContainsKey(CVT)) {
			return (VariantStore[CVT]);
		}
		return (0);
	}
	public int ReturnVariant(int VI)
	{
		if (VariantList.Count > VI)
		{
			return (VariantList[VI]);
		}
		return (0);
	}
}

public enum ClothingVariantType { 
	Default, 
	Tucked,
	Skirt
}