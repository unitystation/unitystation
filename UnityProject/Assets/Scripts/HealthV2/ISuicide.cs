using System.Collections;
using UnityEngine;

namespace HealthV2
{
	/// <summary>
	/// Used to check if a player can suicide.
	/// </summary>
	public interface ISuicide
	{
		/// <summary>
		/// Can this performer suicide? What are the conditions for him to be able to do this?
		/// </summary>
		public bool CanSuicide(GameObject performer);

		/// <summary>
		/// What happens when they can have the ability to do this?
		/// </summary>
		public IEnumerator OnSuicide(GameObject performer);
	}
}

/// NOTE FROM MAX ///
/// we can use this to create an antag or mob in the feature that can ///
/// make use of this interface to kill players and make their deaths ///
/// look like suicides which will be good for detective gameplay ///
/// and ghost hunting for the chaplain. Don't forget about mind breakers as well! ///