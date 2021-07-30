using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace Items.Food
{
	public class HealableJobEdible : Edible
	{
		[SerializeField, Tooltip("The amount of brute damage healed for a player who has a specfied role.")]
		private int healBruteDamage = 5;

		[SerializeField]
		private List<JobType> healableJobs;

		/// <summary>
		/// Checks if the player eating this item has a job that's in healableJobs.
		/// If the player does not have one, nothing special will be triggered.
		/// </summary>
		public override void Eat(PlayerScript eater, PlayerScript feeder)
		{
			if (CheckForJob(eater))
			{
				Heal(eater);
			}
			base.Eat(eater, feeder);
		}

		/// <summary>
		/// Heals all body parts that have brute damage.
		/// </summary>
		private void Heal(PlayerScript player)
		{
			var livingHealth = player.GetComponent<LivingHealthMasterBase>();
			foreach (BodyPart BodyPart in livingHealth.BodyPartList)
			{
				if (BodyPart.Brute > 0)
				{
					BodyPart.HealDamage(this.gameObject, healBruteDamage, DamageType.Brute);
				}
			}
		}

		/// <summary>
		/// Checks if the player has a healable job.
		/// </summary>
		private bool CheckForJob(PlayerScript JobHolder)
		{
			return healableJobs.Contains(JobHolder.mind.occupation.JobType);
		}
	}
}
