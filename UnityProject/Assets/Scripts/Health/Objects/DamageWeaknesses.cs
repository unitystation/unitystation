using System;
using UnityEngine;

namespace Health.Objects
{
	[Serializable]
	public class DamageWeaknesses
	{
		//TODO Some time cap Above zero, Only in the GetRating, Since we need the minus values for stuff like toxic resistance to add and remove properly
		[Range(0, 10)] public float Brute = 1f;
		[Range(0,10)] public float Burn= 1f;
		[Range(0,10)] public float Tox= 1f;
		[Range(0,10)] public float Oxy= 1f;
		[Range(0,10)] public float Clone= 1f;
		[Range(0,10)] public float Stamina= 1f;
		[Range(0,10)] public float Radiation= 1f;

		public float CalculateAppliedDamage(float inDamage, DamageType attackType)
		{
			return inDamage * GetRating(attackType);
		}


		private float GetRating(DamageType attackType)
		{
			switch (attackType)
			{
				case DamageType.Brute:
					return Brute;
				case DamageType.Burn:
					return Burn;
				case DamageType.Tox:
					return Tox;
				case DamageType.Oxy:
					return Oxy;
				case DamageType.Clone:
					return Clone;
				case DamageType.Stamina:
					return Stamina;
				case DamageType.Radiation:
					return Radiation;
			}

			return 1;
		}

	}
}
