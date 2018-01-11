using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public abstract class HealthBehaviour : InputTrigger
{
	//For meat harvest (pete etc)
	public bool allowKnifeHarvest;

	public int initialHealth = 100;

	public bool isNPC;

	public int maxHealth = 100;

	public int Health { get; private set; }

	public DamageType LastDamageType { get; private set; }

	public GameObject LastDamagedBy { get; private set; }

	public ConsciousState ConsciousState { get; protected set; }

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
			Debug.LogWarningFormat("Initial health ({0}) set to zero/below zero!", initialHealth);
			initialHealth = 1;
		}

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

	///fixme/todo: to be replaced by net messages, crappy and unsecure placeholder
	[ClientRpc]
	public void RpcApplyDamage(GameObject damagedBy, int damage,
		DamageType damageType, BodyPartType bodyPartAim)
	{
		if (isServer || !isNPC || IsDead)
		{
			return;
		}
		ApplyDamage(damagedBy, damage, damageType, bodyPartAim);
	}

	public void ApplyDamage(GameObject damagedBy, int damage,
		DamageType damageType, BodyPartType bodyPartAim = BodyPartType.CHEST)
	{
		if (damage <= 0 || IsDead)
		{
			return;
		}
		int calculatedDamage = ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);

		//        Debug.LogFormat("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}",
		//            calculatedDamage, Health, Health - calculatedDamage, gameObject.name, damageType, bodyPartAim, damagedBy);
		Health -= calculatedDamage;
		CheckDeadCritStatus();
	}

	public virtual int ReceiveAndCalculateDamage(GameObject damagedBy, int damage, DamageType damageType,
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

	/// <summary>
	///     make player unconscious upon crit
	/// </summary>
	protected virtual void OnCritActions()
	{
		if (!isNPC)
		{
			PlayerNetworkActions pna = GetComponent<PlayerNetworkActions>();
			if (pna == null)
			{
				Debug.LogError("This is not a player, please set isNPC flag correctly");
				return;
			}
			if (isServer)
			{
				pna?.CmdConsciousState(false);
			}
		}
	}

	protected abstract void OnDeathActions();
	
	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		if (UIManager.Hands.CurrentSlot.Item != null && PlayerManager.PlayerInReach(transform))
		{
			if (UIManager.Hands.CurrentSlot.Item.GetComponent<ItemAttributes>().type == ItemType.Knife)
			{
				Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;
	
				PlayerScript lps = PlayerManager.LocalPlayerScript;
				lps.weaponNetworkActions.CmdKnifeAttackMob(gameObject, UIManager.Hands.CurrentSlot.Item, dir,
					UIManager.DamageZone);
			}
		}
	}
}

public static class HealthThreshold
{
	public const int Crit = 30;
	public const int Dead = 0;
}