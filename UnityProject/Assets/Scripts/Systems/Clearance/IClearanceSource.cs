using System.Collections.Generic;
using System.Linq;

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
		IEnumerable<Clearance> GetCurrentClearance => GameManager.Instance.CentComm.IsLowPop ? IssuedClearance.Concat(LowPopIssuedClearance).Distinct() : IssuedClearance;

		/// <summary>
		/// Issued clearance for this clearance source. This list is consulted when the current round has a normal amount of population.
		/// </summary>
		IEnumerable<Clearance> IssuedClearance { get; }

		/// <summary>
		/// Extra issued clearance for this source. When the round is lowpop, this list is consulted in addition to the normal
		/// </summary>
		IEnumerable<Clearance> LowPopIssuedClearance { get; }
	}
}