using UnityEngine;
using Chemistry;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "BloodType", menuName = "ScriptableObjects/Health/BloodType", order = 0)]
	public class BloodType : Reagent
	{
		[Tooltip("This is the reagent actually metabolised and circulated through this circulatory system.")]
		public Chemistry.Reagent CirculatedReagent;	//Just one for now feel free to add the code for more if needed

		// A, B, O, etc.  Don't know how alien blood types will work, but you can add anything over there.
		public BloodTypes Type;

		///<summary>
		/// The capacity of blood for the circulated reagent, in humans for oxygen this is 0.2
		///</summary>
		public float BloodCapacityOf;

		// Somewhat arbitrary, for humans its calculable per gas by: Moles = Henry's Constant for the Gas / Atmospheric Pressure
		// Used nitrogen at sea level to get 0.0005, could expand this for realism later, but would make non-human blood challenging
		///<summary>
		/// The moles of gas / litre of blood that will disolve in the blood plasma, 0.0005 for humans
		///</summary>
		public float BloodGasCapability;


		public float GetGasCapacityOfnonMeanCarrier(ReagentMix reagentMix)
		{
			return reagentMix[this] * BloodGasCapability;
		}


		public float GetGasCapacity(ReagentMix reagentMix, Reagent reagent = null)
		{
			if (reagent == CirculatedReagent || reagent == null)
			{
				return reagentMix[this] * BloodCapacityOf;
			}
			return reagentMix[this] * BloodGasCapability;
		}

		public float GetSpareGasCapacity(ReagentMix reagentMix, Reagent reagent = null)
		{
			if (reagent == CirculatedReagent || reagent == null)
			{
				return GetGasCapacity(reagentMix) - reagentMix[CirculatedReagent];
			}
			return GetGasCapacity(reagentMix, reagent) - reagentMix[reagent];
		}

		public float GetGasCapacityForeign(ReagentMix reagentMix, Reagent reagent = null)
		{
			float toReturn = 0;
			lock (reagentMix.reagents)
			{
				foreach(var reagen in reagentMix.reagents.m_dict)
				{
					var kindOfBlood = reagen.Key as BloodType;
					if(kindOfBlood != null && kindOfBlood != this)
					{
						toReturn += kindOfBlood.GetGasCapacity(reagentMix, reagent);
					}
				}
			}
			return toReturn;
		}
		public float GetSpareGasCapacityForeign(ReagentMix reagentMix, Reagent reagent = null)
		{
			float toReturn = 0;
			lock (reagentMix.reagents)
			{
				foreach (var reagen in reagentMix.reagents.m_dict)
				{
					var kindOfBlood = reagen.Key as BloodType;
					if (kindOfBlood != null && kindOfBlood != this)
					{
						toReturn += kindOfBlood.GetSpareGasCapacity(reagentMix, reagent);
					}
				}
			}

			return toReturn;
		}
	}
}