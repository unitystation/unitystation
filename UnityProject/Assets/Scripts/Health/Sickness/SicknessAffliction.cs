using System.Collections.Generic;
using UnityEngine;

namespace Health.Sickness
{
	/// <summary>
	/// An occurence of a sickness for a particular afflicted individual
	/// </summary>
	public class SicknessAffliction: MonoBehaviour
	{

		/// <summary>
		/// Indicates that the sickness is healed and will be removed from the player
		/// </summary>
		public bool IsHealed { get; private set; } = false;
		public float ContractedTime { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sicknessToAdd">The sickness to create</param>
		/// <param name="sicknessContractedTime">The time at which the player contracted the sickness.</param>
		public SicknessAffliction(Sickness sicknessToAdd, float sicknessContractedTime)
		{
			ContractedTime = sicknessContractedTime;
			Sickness = sicknessToAdd;
		}

        /// <summary>
        /// The sickness affecting the player
        /// </summary>
        public Sickness Sickness { get; } = null;

        /// <summary>
		/// Mark the current sickness for deletion.
		/// </summary>
		/// <remarks>This method is Thread Safe</remarks>
		public void Heal()
		{
			IsHealed = true;
		}
	}
}
