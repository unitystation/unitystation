using UnityEngine;
using Chemistry;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "BloodType", menuName = "ScriptableObjects/Health/BloodType", order = 0)]
	public class BloodType : Reagent
	{
		public float PercentageNeededInBloodFlow = 0.10f;

		[Tooltip("This is the reagent actually metabolised and circulated through this circulatory system.")]
		public Chemistry.Reagent CirculatedReagent;	//Just one for now feel free to add the code for more if needed

		public Chemistry.Reagent WasteCarryReagent;

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

		[Tooltip("When saturation of blood reagent falls below this point you'll start to feel symptoms, like being light headed.")]
		public float BLOOD_REAGENT_SATURATION_OKAY = 0.80f;

		[Tooltip("When saturation of blood reagent falls below this point the organism will start taking oxy damage.")]
		public float BLOOD_REAGENT_SATURATION_BAD = 0.70f;

		[Tooltip("If we reach critical, the organism will very quickly accumalate oxy damage.")]
		public float BLOOD_REAGENT_SATURATION_CRITICAL = 0.50f;


		public float GetGasCapacity(ReagentMix reagentMix, Reagent reagent)
		{
			if (reagent == CirculatedReagent || reagent == null)
			{
				return GetNormalGasCapacity(reagentMix);
			}
			return GetSpecialGasCapacity(reagentMix);
		}

		public float GetNormalGasCapacity(ReagentMix reagentMix)
		{
			return reagentMix[this] * BloodGasCapability;
		}

		public float GetSpecialGasCapacity(ReagentMix reagentMix)
		{
			return reagentMix[this] * BloodCapacityOf;
		}

		public float GetSpareGasCapacity(ReagentMix reagentMix, Reagent reagent = null)
		{
			if (reagent == CirculatedReagent || reagent == null)
			{
				return GetSpecialGasCapacity(reagentMix) - reagentMix[CirculatedReagent];
			}
			return GetGasCapacity(reagentMix, reagent) - reagentMix[reagent];
		}


		public float CalculatePercentageBloodPresent(ReagentMix reagentMix)
		{
			if (reagentMix.Total.Approx(0))
			{
				return 0;
			}
			var Percentage = (reagentMix[this] / reagentMix.Total);
			if (PercentageNeededInBloodFlow > Percentage)
			{
				return 1;
			}
			else
			{
				return (Percentage) / PercentageNeededInBloodFlow;
			}
		}

		public float BloodSaturation(ReagentMix reagentMix, Reagent reagent)
		{
			float bloodCap = this.GetGasCapacity(reagentMix, reagent);
			if (bloodCap > 0)
			{
				return reagentMix[reagent] / bloodCap;
			}

			return 0;
		}
	}
}