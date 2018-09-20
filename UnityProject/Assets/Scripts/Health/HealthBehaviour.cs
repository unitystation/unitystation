using UnityEngine;
using UnityEngine.Networking;

public abstract class HealthBehaviour : InputTrigger
{
	//For meat harvest (pete etc)
	public bool allowKnifeHarvest;

	public int initialHealth = 100;

	public bool isNotPlayer;

	public int maxHealth = 100;

	public float Health { get; private set; }

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
			Logger.LogWarning($"Initial health ({initialHealth}) set to zero/below zero!", Category.Health);
			initialHealth = 1;
		}

		Health = initialHealth;
	}

	[Server]
	public void ServerOnlySetHealth(float newValue)
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
		if (isServer || !isNotPlayer || IsDead)
		{
			return;
		}
		ApplyDamage(damagedBy, damage, damageType, bodyPartAim);
	}

	public void ApplyDamage(GameObject damagedBy, float damage,
		DamageType damageType, BodyPartType bodyPartAim = BodyPartType.CHEST)
	{
		if (damage <= 0 || IsDead)
		{
			return;
		}
		float calculatedDamage = ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);

		Logger.LogTraceFormat("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
		calculatedDamage, Health, Health - calculatedDamage, gameObject.name, damageType, bodyPartAim, damagedBy);
		Health -= calculatedDamage;
		CheckDeadCritStatus();
	}

	public virtual float ReceiveAndCalculateDamage(GameObject damagedBy, float damage, DamageType damageType,
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

	protected virtual void OnCritActions()
	{
	}

	protected abstract void OnDeathActions();

	//TODO move to p2pinteractions?
	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		if (UIManager.Hands.CurrentSlot.Item != null)
		{
			var handItem = UIManager.Hands.CurrentSlot.Item.GetComponent<ItemAttributes>();
			if (handItem.itemType != ItemType.ID
			    && handItem.itemType != ItemType.Back
			    && handItem.itemType != ItemType.Ear
			    && handItem.itemType != ItemType.Food
			    && handItem.itemType != ItemType.Glasses
			    && handItem.itemType != ItemType.Gloves
			    && handItem.itemType != ItemType.Hat
			    && handItem.itemType != ItemType.Mask
			    && handItem.itemType != ItemType.Neck
			    && handItem.itemType != ItemType.Shoes
			    && handItem.itemType != ItemType.Suit
			    && handItem.itemType != ItemType.Uniform
				&& PlayerManager.PlayerInReach(transform))
			{
				if (UIManager.CurrentIntent == Intent.Attack
				    || handItem.itemType != ItemType.Gun
				    || handItem.itemType != ItemType.Knife
				    || handItem.itemType != ItemType.Belt)
				{
					Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;

					PlayerScript lps = PlayerManager.LocalPlayerScript;
					lps.weaponNetworkActions.CmdRequestMeleeAttack(gameObject, UIManager.Hands.CurrentSlot.eventName, dir,
						UIManager.DamageZone);
				}
			}
		}
	}
}

public static class HealthThreshold
{
	public const int Crit = 30;
	public const int Dead = 0;
}