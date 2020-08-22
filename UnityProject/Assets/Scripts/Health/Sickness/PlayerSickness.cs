using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Health.Sickness
{
	[Serializable]
	public class PlayerSickness : MonoBehaviour
	{
		public PlayerHealth playerHealth;
		public List<SicknessAffliction> sicknessAfflictions;

		public PlayerSickness()
		{
			sicknessAfflictions = new List<SicknessAffliction>();
		}

		private void Awake()
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
			sicknessAfflictions.Add(new SicknessAffliction(sickness, contractedTime));

			// Register the player as a sick player
			SicknessManager.Instance.RegisterSickPlayer(this);
		}
	}
}
