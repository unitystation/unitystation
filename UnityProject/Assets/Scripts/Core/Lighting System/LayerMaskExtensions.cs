using UnityEngine;

public static class LayerMaskExtensions
{
	public static bool HasLayer(this LayerMask layerMask, int layer)
	{
		if (layerMask == (layerMask | (1 << layer)))
		{
			return true;
		}
 
		return false;
	}
 
	public static bool HasAny(this LayerMask layerMask, LayerMask iTestLayers)
	{
		for (int i = 0; i < 32; i++)
		{
			if (iTestLayers == (iTestLayers | (1 << i)))
			{
				if (layerMask.HasLayer(i))
				{
					return true;
				}
			}
		}
 
		return false;
	}
}