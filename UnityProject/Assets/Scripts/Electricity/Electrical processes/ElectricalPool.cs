using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalPool 
{
	public static List<ResistanceWrap> PooledResistanceWraps = new List<ResistanceWrap>();
	public static ResistanceWrap GetResistanceWrap()
	{
		if (PooledResistanceWraps.Count > 0)
		{
			var ResistanceWrap = PooledResistanceWraps[0];
			PooledResistanceWraps.RemoveAt(0);
			return (ResistanceWrap);
		}
		else {
			return (new ResistanceWrap());
		}

	}  

}
