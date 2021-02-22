using System.Collections;
using UnityEngine;


public class Donut : Edible
{
	[Tooltip("The amount of brute damage healed for a player who has a secuirty role.")]
	[SerialiezedField]
	private int securityHealBruteDamage = 5;


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
		Eat(eater, feeder, NutritionLevel);
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
				BodyPart.HealDamage(securityHealBruteDamage, DamageType.Brute);
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
	private void Eat(PlayerScript eater, PlayerScript feeder, int nutrition)
	{
		SoundManager.PlayNetworkedAtPos(eatSound, eater.WorldPos, sourceObj: eater.gameObject);

		eater.playerHealth.Metabolism.AddEffect(new MetabolismEffect(nutrition, 0, MetabolismDuration.Food));

		var feederSlot = feeder.ItemStorage.GetActiveHandSlot();
		Inventory.ServerDespawn(gameObject);

		if (leavings != null)
		{
			var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
			var pickupable = leavingsInstance.GetComponent<Pickupable>();
			bool added = Inventory.ServerAdd(pickupable, feederSlot);
			if (!added)
			{
				//If stackable has leavings and they couldn't go in the same slot, they should be dropped
				pickupable.CustomNetTransform.SetPosition(feeder.WorldPos);
			}
		}
	}
}

