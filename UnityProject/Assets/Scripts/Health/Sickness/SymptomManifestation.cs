namespace Health.Sickness
{
	/// <summary>
	/// A symptom to be manifested
	/// </summary>
	public class SymptomManifestation
	{
		public SicknessAffliction SicknessAffliction;
		public int Stage;
		public PlayerHealth PlayerHealth;

		public SymptomManifestation(SicknessAffliction sicknessAffliction, int stage, PlayerHealth playerHealth)
		{
			SicknessAffliction = sicknessAffliction;
			Stage = stage;
			PlayerHealth = playerHealth;
		}
	}
}
