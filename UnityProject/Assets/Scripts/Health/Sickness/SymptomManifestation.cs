using HealthV2;

namespace Health.Sickness
{
	/// <summary>
	/// A symptom to be manifested
	/// </summary>
	public class SymptomManifestation
	{
		public SicknessAffliction SicknessAffliction;
		public int Stage;
		public LivingHealthMasterBase MobHealth;

		public SymptomManifestation(SicknessAffliction sicknessAffliction, int stage, LivingHealthMasterBase livingHealth)
		{
			SicknessAffliction = sicknessAffliction;
			Stage = stage;
			MobHealth = livingHealth;
		}
	}
}
