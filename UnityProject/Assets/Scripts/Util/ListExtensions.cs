using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions 
{
  	public static void RemoveEndsOfList<T>(List<T> list, int NumberElements)
	{
		list.RemoveRange((list.Count - 1) - NumberElements, NumberElements);
	}

	public static void RemoveAtIndexForwards<T>(List<T> list, int Index)
	{
		if (list.Count > 0 && (!((list.Count - 1) < Index)))
		{
			list.RemoveRange(Index, (list.Count - 1) - Index);
		}
	}
}
