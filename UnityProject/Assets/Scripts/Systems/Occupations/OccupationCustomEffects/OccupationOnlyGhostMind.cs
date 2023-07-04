using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class OccupationOnlyGhostMind : OccupationCustomEffectBase, IGetPlayerPrefab
{
	public virtual  GameObject GetPlayerPrefab()
	{
		return null;
	}
}
