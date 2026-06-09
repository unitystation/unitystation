namespace US13.Systems.InGameEvents
{
	/// <summary>
	/// Parameters for the sickness event
	/// </summary>
	public class SicknessEventParameters: BaseEventParameters
	{
		/// <summary>
		/// Number of players to infect
		/// </summary>
		public int PlayerToInfect;

		/// <summary>
		/// The index of the sickness to apply in CureManager.CureableSicknesses
		/// </summary>
		public int SicknessIndex;


		/// <summary>
		/// The starting % of the disease in applied victims
		/// </summary>
		public float Strength;
	}
}
