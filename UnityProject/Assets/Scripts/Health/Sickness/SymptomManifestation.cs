namespace Health.Sickness
{
	/// <summary>
	/// A symptom to be manifested
	/// </summary>
	public class SymptomManifestation
	{
		public SicknessAffliction SicknessAffliction;
		public int Stage;
		public PlayerHealthV2 PlayerHealth;

		public SymptomManifestation(SicknessAffliction sicknessAffliction, int stage, PlayerHealthV2 playerHealth)
		{
			SicknessAffliction = sicknessAffliction;
			Stage = stage;
			PlayerHealth = playerHealth;
		}
	}
}
