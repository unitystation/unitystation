using System.Collections.Generic;

namespace Systems.Clearance
{
	/// <summary>
	/// Interface that represents a clearance source, like an ID card for example.
	/// </summary>
	public interface IClearanceSource
	{
		/// <summary>
		/// Property with default implementation that returns the relevant clearance for this source depending on
		/// if the round is currently a lowpop round or not.
		/// </summary>
		/// <returns>Current relevant clearance</returns>
		IEnumerable<Clearance> GetCurrentClearance => GameManager.Instance.CentComm.IsLowPop ? LowPopIssuedClearance : IssuedClearance;

		/// <summary>
		/// Issued clearance for this clearance source. This list is consulted when the current round has a normal amount of population.
		/// </summary>
		IEnumerable<Clearance> IssuedClearance { get; }

		/// <summary>
		/// Issued clearance for this source. This list is consulted when the current round has low population.
		/// </summary>
		IEnumerable<Clearance> LowPopIssuedClearance { get; }
	}
}