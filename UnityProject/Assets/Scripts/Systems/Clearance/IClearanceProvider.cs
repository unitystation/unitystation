using System.Collections.Generic;

namespace Systems.Clearance
{
	public interface IClearanceProvider
	{
		/// <summary>
		/// Public interface to get current issued clearance on this object.
		/// </summary>
		/// <returns>list with all clearances</returns>
		IEnumerable<Clearance> GetClearance();
	}
}