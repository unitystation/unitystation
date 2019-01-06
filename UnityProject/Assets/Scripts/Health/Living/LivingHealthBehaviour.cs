using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class LivingHealthBehaviour : NetworkBehaviour
{
	public int maxHealth = 100;

	public int Health { get; private set; }

	protected BloodSystem bloodSystem;

	/// <summary>
	/// If there are any body parts for this living thing, then add them to this list
	/// via the inspector. There needs to be at least 1 chest bodypart for a living animal
	/// </summary>
	[Header("Fill BodyPart fields in via Inspector:")]
	public List<BodyPartBehaviour> BodyParts = new List<BodyPartBehaviour>();

	//For meat harvest (pete etc)
	public bool allowKnifeHarvest;

	[Header("For harvestable animals")]
	public GameObject[] butcherResults;

	[Header("Is this an animal or NPC?")]
	public bool isNotPlayer = false;

	protected DamageType LastDamageType;

	protected GameObject LastDamagedBy;

	public ConsciousState ConsciousState { get; protected set; }

	// JSON string for blood types and DNA.
	[SyncVar(hook = "DNASync")] //May remove this in the future and only provide DNA info on request
	private string DNABloodTypeJSON;

	// BloodType and DNA Data.
	private DNAandBloodType DNABloodType;

	//be careful with falses, will make player conscious
	public bool IsCrit
	{
		get { return ConsciousState == ConsciousState.UNCONSCIOUS; }
		private set { ConsciousState = value ? ConsciousState.UNCONSCIOUS : ConsciousState.CONSCIOUS; }
	}

	public bool IsDead
	{
		get { return ConsciousState == ConsciousState.DEAD; }
		private set { ConsciousState = value ? ConsciousState.DEAD : ConsciousState.CONSCIOUS; }
	}

	void Awake()
	{
		bloodSystem = GetComponent<BloodSystem>();
		if (bloodSystem == null)
		{
			bloodSystem = gameObject.AddComponent<BloodSystem>();
		}
	}

	public override void OnStartServer()
	{
		ResetBodyParts();
		if (maxHealth <= 0)
		{
			Logger.LogWarning($"Max health ({maxHealth}) set to zero/below zero!", Category.Health);
			maxHealth = 1;
		}
		Health = maxHealth;

		//Generate BloodType and DNA
		DNABloodType = new DNAandBloodType();
		DNABloodTypeJSON = JsonUtility.ToJson(DNABloodType);
		bloodSystem.SetBloodType(DNABloodType);
		base.OnStartServer();
	}

	public override void OnStartClient()
	{
		StartCoroutine(WaitForClientLoad());
		base.OnStartClient();
	}

	IEnumerator WaitForClientLoad()
	{
		//wait for DNA:
		while (string.IsNullOrEmpty(DNABloodTypeJSON))
		{
			yield return YieldHelper.EndOfFrame;
		}
		yield return YieldHelper.EndOfFrame;
		DNASync(DNABloodTypeJSON);
	}

	/// <summary>
	/// Reset all body part damage.
	/// </summary>
	[Server]
	private void ResetBodyParts()
	{
		foreach (BodyPartBehaviour bodyPart in BodyParts)
		{
			bodyPart.RestoreDamage();
		}
	}

	[Server]
	public void ServerOnlySetHealth(int newValue)
	{
		if (isServer)
		{
			Health = newValue;
			CheckDeadCritStatus();
		}
	}

	// This is the DNA SyncVar hook
	private void DNASync(string updatedDNA)
	{
		DNABloodTypeJSON = updatedDNA;
		DNABloodType = JsonUtility.FromJson<DNAandBloodType>(updatedDNA);
	}

	/// <summary>
	///  Apply Damage to the Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount</param>
	/// <param name="damageType">The Type of Damage</param>
	/// <param name="bodyPartAim">Body Part that is affected</param>
	[Server]
	public virtual void ApplyDamage(GameObject damagedBy, int damage,
		DamageType damageType, BodyPartType bodyPartAim = BodyPartType.CHEST)
	{
		if (damage <= 0 || IsDead)
		{
			return;
		}
		if (bodyPartAim == BodyPartType.GROIN)
		{
			bodyPartAim = BodyPartType.CHEST; //Temporary fix for groin, when we add surgery this might need some changing.
		}

		LastDamageType = damageType;
		LastDamagedBy = damagedBy;

		if (BodyParts.Count == 0)
		{
			Logger.LogError($"There are no body parts to apply damage too for {gameObject.name}", Category.Health);
			return;
		}

		if (damageType == DamageType.BRUTE || damageType == DamageType.BURN)
		{
			//Try to apply damage to the required body part
			bool appliedDmg = false;
			for (int i = 0; i < BodyParts.Count; i++)
			{
				if (BodyParts[i].Type == bodyPartAim)
				{
					BodyParts[i].ReceiveDamage(damageType, damage);
					appliedDmg = true;
					break;
				}
			}

			//If the body part does not exist then try to find the chest instead
			if (!appliedDmg)
			{
				var getChestIndex = BodyParts.FindIndex(x => x.Type == BodyPartType.CHEST);
				if (getChestIndex != -1)
				{
					BodyParts[getChestIndex].ReceiveDamage(damageType, damage);
				}
				else
				{
					//If there is no default chest body part then do nothing
					Logger.LogError($"No chest body part found for {gameObject.name}", Category.Health);
					return;
				}
			}
		}
		else
		{
			//TODO: Could be Toxin Damage or lack of oxygen:
		}

		//For special effects spawning like blood:
		DetermineDamageEffects(damageType);

		var prevHealth = Health;
		CalculateOverallHealth();

		Logger.LogTraceFormat("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
			damage, prevHealth, Health, gameObject.name, damageType, bodyPartAim, damagedBy);

		//	int calculatedDamage = ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);
		//	Health -= calculatedDamage;
	}

	/// <summary>
	///  Apply healing to a living thing. Server Only
	/// </summary>
	/// <param name="healingItem">the item used for healing (bruise pack etc). Null if there is none</param>
	/// <param name="healAmt">Amount of healing to add</param>
	/// <param name="damageType">The Type of Damage To Heal</param>
	/// <param name="bodyPartAim">Body Part to heal</param>
	[Server]
	public virtual void HealDamage(GameObject healingItem, int healAmt,
		DamageType damageTypeToHeal, BodyPartType bodyPartAim)
	{
		if (healAmt <= 0 || IsDead)
		{
			return;
		}
		if (bodyPartAim == BodyPartType.GROIN)
		{
			bodyPartAim = BodyPartType.CHEST; //Temporary fix for groin, when we add surgery this might need some changing.
		}

		if (BodyParts.Count == 0)
		{
			Logger.LogError($"There are no body parts to apply damage too for {gameObject.name}", Category.Health);
			return;
		}

		if (damageTypeToHeal == DamageType.BRUTE || damageTypeToHeal == DamageType.BURN)
		{
			//Try to apply healing to the required body part
			bool appliedHealing = false;
			for (int i = 0; i < BodyParts.Count; i++)
			{
				if (BodyParts[i].Type == bodyPartAim)
				{
					BodyParts[i].HealDamage(healAmt, damageTypeToHeal);
					appliedHealing = true;
					break;
				}
			}

			//If the body part does not exist then try to find the chest instead
			if (!appliedHealing)
			{
				var getChestIndex = BodyParts.FindIndex(x => x.Type == BodyPartType.CHEST);
				if (getChestIndex != -1)
				{
					BodyParts[getChestIndex].HealDamage(healAmt, damageTypeToHeal);
				}
				else
				{
					//If there is no default chest body part then do nothing
					Logger.LogError($"No chest body part found for {gameObject.name}", Category.Health);
					return;
				}
			}
		}
		else
		{
			//TODO: Could be Oxygen or Toxin healing
		}

		var prevHealth = Health;
		CalculateOverallHealth();

		Logger.LogTraceFormat("{3} received {0} {4} healing from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
			healAmt, prevHealth, Health, gameObject.name, damageTypeToHeal, bodyPartAim, healingItem);
	}

	/// <Summary>
	/// Used to determine any special effects spawning cased by a damage type
	/// Server only
	/// </Summary>
	[Server]
	protected virtual void DetermineDamageEffects(DamageType damageType)
	{
		//Brute attacks
		if (damageType == DamageType.BRUTE)
		{
			//spawn blood
			EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.medium);
		}
	}

	/// <summary>
	/// Recalculates the overall player health. Server only
	/// </summary>
	[Server]
	protected void CalculateOverallHealth()
	{
		CheckDeadCritStatus();
	}

	///Death from other causes
	public virtual void Death()
	{
		if (IsDead)
		{
			return;
		}
		IsDead = true;
		Health = HealthThreshold.Dead;
		OnDeathActions();
	}

	public virtual void Crit()
	{
		if (ConsciousState != ConsciousState.CONSCIOUS)
		{
			return;
		}
		IsCrit = true;
		OnCritActions();
	}

	private void CheckDeadCritStatus()
	{
		if (Health < HealthThreshold.Crit)
		{
			Crit();
		}
		if (NotSuitableForDeath())
		{
			return;
		}
		Death();
	}

	private bool NotSuitableForDeath()
	{
		return Health > HealthThreshold.Dead || IsDead;
	}

	//FIXME: This must be converted into a method to alleviate hunger soon
	public void AddHealth(int amount)
	{
		Debug.Log("TODO PRIORITY: Food should no longer heal, instead it should cure hunger");
		if (amount <= 0)
		{
			return;
		}
		Health += amount;

		if (Health > maxHealth)
		{
			Health = maxHealth;
		}
	}

	protected BodyPartBehaviour FindBodyPart(BodyPartType bodyPartAim)
	{
		for (int i = 0; i < BodyParts.Count; i++)
		{
			if (BodyParts[i].Type == bodyPartAim)
			{
				return BodyParts[i];
			}
		}
		//dm code quotes:
		//"no bodypart, we deal damage with a more general method."
		//"missing limb? we select the first bodypart (you can never have zero, because of chest)"
		return BodyParts.PickRandom();
	}

	protected virtual void OnCritActions() { }

	protected abstract void OnDeathActions();

	///<summary>
	/// If Harvesting is allowed (for pete the goat for example)
	/// then spawn the butchered results
	/// </summary>
	[Server]
	public void Harvest()
	{
		foreach (GameObject harvestPrefab in butcherResults)
		{
			ItemFactory.SpawnItem(harvestPrefab, transform.position, transform.parent);
		}
		EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.medium);
		//Remove the NPC after all has been harvested
		ObjectBehaviour objectBehaviour = gameObject.GetComponent<ObjectBehaviour>();
		objectBehaviour.visibleState = false;
	}
}

public static class HealthThreshold
{
	public const int Crit = 30;
	public const int Dead = 0;
}