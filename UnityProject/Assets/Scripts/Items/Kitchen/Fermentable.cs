using System;
using UnityEngine;
using NaughtyAttributes;
using Chemistry;


namespace Items.Food
{
	/// <summary>
	/// This class doesn't handle cooking itself; it merely stores the cooking times
	/// and products for the fermenting barrel to use when fermenting.
	/// <para>The <see cref="OnFerment"/> event is raised when something ferments this, which other
	/// components can subscribe to, to perform extra logic (for e.g. making unique wines).</para>
	/// </summary>
	public class Fermentable : MonoBehaviour
	{
		[Tooltip("Minimum time to ferment.")]
		public int FermentTime = 10;

		[SerializeField]
		[Tooltip("What reagent(s) this GameObject becomes when fermented.")]
		private SerializableDictionary<Reagent, int> fermentedReagents;
		/// <summary>
		/// Get the processed product of this object.
		/// </summary>
		public SerializableDictionary<Reagent, int> FermentedReagents => fermentedReagents;

		/// <summary>
		/// Raised when enough fermenting time has been added (via <see cref="AddFermentingTime(float)"/>)
		/// </summary>
		public event Action OnFerment;

		private float timeSpentFermenting;

		/// <summary>
		/// Adds the given fermenting time to this object. Will return true if the item is now fermented.
		/// </summary>
		/// <param name="time">The amount of time in seconds to add to this object's time spent fermenting.</param>
		/// <returns>true if the added time and any previous time spent fermenting was enough to exceed the required fermenting time.</returns>
		public bool AddFermentingTime(float time)
		{
			timeSpentFermenting += time;
			if (timeSpentFermenting > FermentTime)
			{
				OnFerment?.Invoke();
				return true;
			}

			return false;
		}
	}
}
