using UnityEngine;
using Chemistry;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "BloodType", menuName = "ScriptableObjects/Health/BloodType", order = 0)]
	public class BloodType : ScriptableObject
	{

		public Chemistry.Reagent Blood;

		[Tooltip("This is the reagent actually metabolised and circulated through this circulatory system.")]
		public Chemistry.Reagent CirculatedReagent;
		//Just one for now feel free to add the code for more if needed


		[Tooltip("The color of this bloodtype.")]
		public Color Color;

		//public CirculatorySystemBase.BloodStat bloodStat;

		public int BloodCapacityOf;

		public float GetCapacity(float AvailableBlood)
		{
			return AvailableBlood * BloodCapacityOf;
		}

		public float GetCapacity(ReagentMix ReagentMix)
		{
			return ReagentMix[Blood] * BloodCapacityOf;
		}

		public float GetSpareCapacity(ReagentMix ReagentMix)
		{
			return (GetCapacity(ReagentMix) * BloodCapacityOf) - ReagentMix[CirculatedReagent];
		}
	}
}