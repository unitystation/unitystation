using System;
using System.Collections.Generic;
using UnityEngine;

namespace Health.Sickness
{
	/// <summary>
	/// The sickness state of a particular player.
	/// </summary>
	[Serializable]
	public class PlayerSickness : MonoBehaviour
	{
		public PlayerHealth playerHealth;
		public List<SicknessAffliction> sicknessAfflictions;

		public PlayerSickness()
		{
			sicknessAfflictions = new List<SicknessAffliction>();
		}

		private void Start()
		{
			playerHealth = GetComponent<PlayerHealth>();
		}

		/// <summary>
		/// Add a sickness to the player
		/// </summary>
		/// <param name="sickness">The sickness to add</param>
		/// <param name="contractedTime">The time at which the player contracted the sickness</param>
		public void Add(Sickness sickness, float contractedTime)
		{
			lock (sicknessAfflictions)
			{
				sicknessAfflictions.Add(new SicknessAffliction(sickness, contractedTime));
			}

			// Register the player as a sick player
			SicknessManager.Instance.RegisterSickPlayer(this);
		}


		/// <summary>
		/// Remove a sickness from the player, healing him.
		/// </summary>
		/// <param name="sickness">The sickness to remove</param>
		public void Remove(Sickness sickness)
		{
			lock (sicknessAfflictions)
			{
				sicknessAfflictions.Remove(sicknessAfflictions.Find(p => p.Sickness == sickness));
			}
		}

		/// <summary>
		/// Check if the player already is infected by this sickness
		/// </summary>
		/// <param name="sickness"></param>
		/// <returns>True if the player already has this sickness active</returns>
		public bool HasSickness(Sickness sickness)
		{
			lock (sicknessAfflictions)
			{
				return sicknessAfflictions.Exists(p => p.Sickness == sickness);
			}
		}
	}
}
