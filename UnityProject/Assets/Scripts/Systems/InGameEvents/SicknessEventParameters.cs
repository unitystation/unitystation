namespace InGameEvents
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
		/// The NetId of the sickness to apply
		/// </summary>
		public int SicknessIndex;
	}
}
