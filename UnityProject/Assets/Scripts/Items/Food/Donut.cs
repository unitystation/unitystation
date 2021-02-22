using System.Collections;
using UnityEngine;


public class Donut : Edible
{
	[SerializeField, Tooltip("The amount of brute damage healed for a player who has a specfied role.")]
	private int healBruteDamage = 5;

	/// <summary>
	/// Checks if the player eating the donut has a secuirty job and heals them if they are part of security.
	/// If the player is not part of secuirty then nothing special will be triggered.
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
		var livingHealth = player.GetComponent<LivingHealthBehaviour>();
		foreach (BodyPartBehaviour BodyPart in livingHealth.BodyParts)
		{
			if(BodyPart.BruteDamage > 0)
			{
				BodyPart.HealDamage(healBruteDamage, DamageType.Brute);
			}
		}
	}

	/// <summary>
	/// Checks if the player has a secuirty job.
	/// </summary>
	private bool CheckForJob(PlayerScript JobHolder)
	{
		switch (JobHolder.mind.occupation.JobType)
		{
			case JobType.SECURITY_OFFICER:
				return true;
			case JobType.HOS:
				return true;
			case JobType.DETECTIVE:
				return true;
			case JobType.WARDEN:
				return true;
			default:
				return false;
		}
	}
}

