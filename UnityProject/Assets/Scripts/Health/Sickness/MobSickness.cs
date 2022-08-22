using System;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace Health.Sickness
{
	/// <summary>
	/// The sickness state of a particular player.
	/// </summary>
	[Serializable]
	[DisallowMultipleComponent]
	public class MobSickness : MonoBehaviour
	{
		private LivingHealthMasterBase mobHealth;
		public LivingHealthMasterBase MobHealth => mobHealth;
		public List<SicknessAffliction> sicknessAfflictions;

		public MobSickness()
		{
			sicknessAfflictions = new List<SicknessAffliction>();
		}

		private void Start()
		{
			mobHealth = GetComponent<LivingHealthMasterBase>();
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

		public void TriggerCustomSicknessLogic()
		{
			lock (sicknessAfflictions)
			{
				foreach (var sickness in sicknessAfflictions)
				{
					sickness.Sickness.SicknessBehavior(mobHealth);
					if (sickness.Sickness.CheckForCureInHealth(MobHealth)) sickness.Heal();
				}
			}
		}
	}
}
