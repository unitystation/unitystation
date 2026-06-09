using UnityEngine;
using US13.Systems.Occupations.OccupationCustomEffects.Interfaces;

namespace US13.Systems.Occupations.OccupationCustomEffects
{
	public class OccupationOnlyGhostMind : OccupationCustomEffectBase, IGetPlayerPrefab
	{
		public virtual  GameObject GetPlayerPrefab()
		{
			return null;
		}
	}
}
