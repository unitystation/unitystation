using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class LivingHealthBehaviour : NetworkBehaviour
{
	public int maxHealth = 100;

	public int OverallHealth { get; private set; } = 100;

	// Systems can also be added via inspector 
	public BloodSystem bloodSystem;
	public BrainSystem brainSystem;
	public RespiratorySystem respiratorySystem;

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
	private float tickRate = 1f;
	private float tick = 0;

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

	/// <summary>
	/// Has the heart stopped.
	/// May not be updated on the client
	/// Request state from the server
	/// </summary>
	public bool IsCadiacArrest
	{
		get { return bloodSystem.HeartStopped; }
	}

	/// <summary>
	/// Has breathing stopped
	/// May not be updated on the client
	/// Request state from the server
	/// </summary>
	public bool IsRespiratoryArrest
	{
		get { return !respiratorySystem.IsBreathing; }
	}

	void Awake()
	{
		InitSystems();
	}

	void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
	}

	void OnDisable()
	{
		if (UpdateManager.Instance != null)
			UpdateManager.Instance.Remove(UpdateMe);
	}

	/// Add any missing systems:
	private void InitSystems()
	{
		//Always include blood for living entities:
		bloodSystem = GetComponent<BloodSystem>();
		if (bloodSystem == null)
		{
			bloodSystem = gameObject.AddComponent<BloodSystem>();
		}

		//Always include respiratory for living entities:
		respiratorySystem = GetComponent<RespiratorySystem>();
		if (respiratorySystem == null)
		{
			respiratorySystem = gameObject.AddComponent<RespiratorySystem>();
		}

		var tryGetHead = FindBodyPart(BodyPartType.Head);
		if (tryGetHead != null && brainSystem == null)
		{
			if (tryGetHead.Type != BodyPartType.Chest)
			{
				//Head exists, install a brain system
				brainSystem = gameObject.AddComponent<BrainSystem>();
			}
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
		OverallHealth = maxHealth;

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
			OverallHealth = newValue;
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
		DamageType damageType, BodyPartType bodyPartAim = BodyPartType.Chest)
	{
		if (damage <= 0 || IsDead)
		{
			return;
		}
		if (bodyPartAim == BodyPartType.Groin)
		{
			bodyPartAim = BodyPartType.Chest; //Temporary fix for groin, when we add surgery this might need some changing.
		}

		if (bodyPartAim == BodyPartType.Eyes || bodyPartAim == BodyPartType.Mouth)
		{
			bodyPartAim = BodyPartType.Head;
		}

		LastDamageType = damageType;
		LastDamagedBy = damagedBy;

		if (BodyParts.Count == 0)
		{
			Logger.LogError($"There are no body parts to apply damage too for {gameObject.name}", Category.Health);
			return;
		}

		//See if damage affects the state of the blood:
		bloodSystem.AffectBloodState(bodyPartAim, damageType, damage);

		if (damageType == DamageType.Brute || damageType == DamageType.Burn)
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
				var getChestIndex = BodyParts.FindIndex(x => x.Type == BodyPartType.Chest);
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

		//For special effects spawning like blood:
		DetermineDamageEffects(damageType);

		var prevHealth = OverallHealth;

		Logger.LogTraceFormat("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
			damage, prevHealth, OverallHealth, gameObject.name, damageType, bodyPartAim, damagedBy);

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
		if (bodyPartAim == BodyPartType.Groin)
		{
			bodyPartAim = BodyPartType.Chest; //Temporary fix for groin, when we add surgery this might need some changing.
		}

		if (BodyParts.Count == 0)
		{
			Logger.LogError($"There are no body parts to affect {gameObject.name}", Category.Health);
			return;
		}

		// See if any of the healing applied affects blood state
		bloodSystem.AffectBloodState(bodyPartAim, damageTypeToHeal, healAmt, true);

		if (damageTypeToHeal == DamageType.Brute || damageTypeToHeal == DamageType.Burn)
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
				var getChestIndex = BodyParts.FindIndex(x => x.Type == BodyPartType.Chest);
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

		var prevHealth = OverallHealth;

		Logger.LogTraceFormat("{3} received {0} {4} healing from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
			healAmt, prevHealth, OverallHealth, gameObject.name, damageTypeToHeal, bodyPartAim, healingItem);
	}

	/// <Summary>
	/// Used to determine any special effects spawning cased by a damage type
	/// Server only
	/// </Summary>
	[Server]
	protected virtual void DetermineDamageEffects(DamageType damageType)
	{
		//Brute attacks
		if (damageType == DamageType.Brute)
		{
			//spawn blood
			EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.medium);
		}
	}

	//Handled via UpdateManager
	void UpdateMe()
	{
		//Server Only:
		if (CustomNetworkManager.Instance._isServer && !IsDead)
		{
			tick += Time.deltaTime;
			if (tick > tickRate)
			{
				tick = 0f;
				CalculateOverallHealth();
			}
		}
	}

	/// HEALTH CALCULATIONS
	/// ---------------------------

	/// <summary>
	/// Recalculates the overall player health and updates OverallHealth property. Server only
	/// </summary>
	[Server]
	protected void CalculateOverallHealth()
	{
		int newHealth = 100;
		newHealth -= CalculateOverallBodyPartDamage();
		newHealth -= CalculateOverallBloodLossDamage();

		//We are in critical state. Add suffocation damage:
		if (newHealth <= 0 || bloodSystem.OxygenLevel < 5)
		{
			//Force into crit state if everything else is fine but there is no oxygen
			if (newHealth > 0)
			{
				newHealth = 0;
			}

			if (respiratorySystem.IsSuffocating)
			{
				newHealth -= respiratorySystem.SuffocationDamage;
			}
		}

		OverallHealth = newHealth;
		// if (GetComponent<PlayerScript>())
		// {
		// 	Debug.Log("Total Overall health " + OverallHealth);
		// }
		CheckDeadCritStatus();
	}

	int CalculateOverallBodyPartDamage()
	{
		float bodyPartDmg = 0;
		for (int i = 0; i < BodyParts.Count; i++)
		{
			if (BodyParts[i].Severity == DamageSeverity.None)
			{
				continue;
			}

			var calc = (float)BodyParts[i].Severity / BodyParts.Count;

			//Head and chest are vital areas, if either one reaches max damage thats automatic crit:
			if (BodyParts[i].Type == BodyPartType.Chest || BodyParts[i].Type == BodyPartType.Head)
			{
				if (BodyParts[i].Severity == DamageSeverity.Max)
				{
					calc = 100;
				}
			}

			bodyPartDmg += calc;
		}
		return Mathf.RoundToInt(Mathf.Clamp(bodyPartDmg, -100f, 100f));
	}

	/// Blood Loss and Toxin damage:
	int CalculateOverallBloodLossDamage()
	{
		float bloodDmg = 0f;
		if (bloodSystem.BloodLevel < (int)BloodVolume.SAFE)
		{
			bloodDmg = (1f - ((float)bloodSystem.BloodLevel / (float)BloodVolume.NORMAL)) * 100f;
		}

		if (bloodSystem.ToxinLevel > 1f)
		{
			//TODO determine a way to handle toxin damage when toxins are implemented
			//There will need to be some kind of blood / toxin ratio and severity limits determined
		}

		return Mathf.RoundToInt(Mathf.Clamp(bloodDmg, 0f, 101f));
	}

	///Death from other causes
	public virtual void Death()
	{
		if (IsDead)
		{
			return;
		}
		IsDead = true;
		OverallHealth = HealthThreshold.Dead;
		OnDeathActions();
		bloodSystem.StopBleeding();
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
		if (OverallHealth <= HealthThreshold.Crit)
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
		return OverallHealth > HealthThreshold.Dead || IsDead;
	}

	//FIXME: This must be converted into a method to alleviate hunger soon
	public void AddHealth(int amount)
	{
		Debug.Log("TODO PRIORITY: Food should no longer heal, instead it should cure hunger");
		if (amount <= 0)
		{
			return;
		}
		OverallHealth += amount;

		if (OverallHealth > maxHealth)
		{
			OverallHealth = maxHealth;
		}
	}

	public BodyPartBehaviour FindBodyPart(BodyPartType bodyPartAim)
	{
		int searchIndex = BodyParts.FindIndex(x => x.Type == bodyPartAim);
		if (searchIndex != -1)
		{
			return BodyParts[searchIndex];
		}
		//If nothing is found then try to find a chest component:
		searchIndex = BodyParts.FindIndex(x => x.Type == BodyPartType.Chest);
		if (searchIndex != -1)
		{
			return BodyParts[searchIndex];
		}
		// else nothing:
		return null;
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
	public const int Crit = 0;
	public const int Dead = -100;
}