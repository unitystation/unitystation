using Chemistry;

namespace US13.HealthV2.Living.MedicalChemistry
{
	public interface IStatusMetabolismReagentSelector
	{
		bool HasMatch(ReagentMix reagentMix);
		void ConsumeMatches(ReagentMix reagentMix, float maxReactQuantity, float metabolismMultiplier);
	}
}
