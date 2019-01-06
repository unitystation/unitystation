using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class LivingHealthBehaviour : NetworkBehaviour
{
	//For meat harvest (pete etc)
	public bool allowKnifeHarvest;

	public int initialHealth = 100;

	public bool isNotPlayer;

	public int maxHealth = 100;

	public int Health { get; private set; }

	protected DamageType LastDamageType;

	protected GameObject LastDamagedBy;

	public ConsciousState ConsciousState { get; protected set; }

	/// <summary>
	/// If there are any body parts for this living thing, then add them to this list
	/// via the inspector
	/// </summary>
	public List<BodyPartBehaviour> BodyParts = new List<BodyPartBehaviour>();

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

	private void OnEnable()
	{
		if (initialHealth <= 0)
		{
			Logger.LogWarning($"Initial health ({initialHealth}) set to zero/below zero!", Category.Health);
			initialHealth = 1;
		}

		//Reset health value and damage types values.
		Health = initialHealth;
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

		for (int i = 0; i < BodyParts.Count; i++)
		{
			if (BodyParts[i].Type == bodyPartAim)
			{
				BodyParts[i].ReceiveDamage(damageType, damage);
			}
		}

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
	/// <param name="bodyPartToHeal">Body Part to heal</param>
	[Server]
	public void HealDamage(GameObject healingItem, int healAmt,
		DamageType damageTypeToHeal, BodyPartType bodyPartToHeal)
	{

	}

	/// <summary>
	/// Recalculates the overall player health. Server only
	/// </summary>
	[Server]
	protected void CalculateOverallHealth()
	{

		CheckDeadCritStatus();
	}

	protected virtual int ReceiveAndCalculateDamage(GameObject damagedBy, int damage, DamageType damageType,
		BodyPartType bodyPartAim)
	{
		LastDamageType = damageType;
		LastDamagedBy = damagedBy;

		return damage;
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

	public void AddHealth(int amount)
	{
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

	public void RestoreHealth()
	{
		Health = initialHealth;
	}

	protected virtual void OnCritActions() { }

	protected abstract void OnDeathActions();
}

public static class HealthThreshold
{
	public const int Crit = 30;
	public const int Dead = 0;
}