using UnityEngine;

public class BodyPartBehaviour : MonoBehaviour
{
	//Different types of damages for medical.
	private int bruteDamage;
	private int burnDamage;
	private int toxinDamage;
	private int suffocationDamage;

	public Sprite GrayDamageMonitorIcon;

	public Sprite GreenDamageMonitorIcon;

	//50 for limbs, 200 for the head and torso(?)
	public int MaxDamage = 50;

	public Sprite OrangeDamageMonitorIcon;
	public Sprite RedDamageMonitorIcon;
	public BodyPartType Type;
	public Sprite YellowDamageMonitorIcon;

	public DamageSeverity Severity { get; private set; }

	//Apply damages from here.
	public virtual void ReceiveDamage(DamageType damageType, int damage)
	{
		UpdateDamage(damage, damageType);
		Logger.LogTraceFormat("{0} received {1} {2} damage. Total {3}/{4}, limb condition is {5}", Category.Health, Type, damage, damageType, damage, MaxDamage, Severity);
	}

	private void UpdateDamage(int damage, DamageType type)
	{
		switch (type)
		{
			case DamageType.BRUTE:
				bruteDamage += damage;

				if (damage > MaxDamage)
				{
					bruteDamage = MaxDamage;
				}
				break;

			case DamageType.BURN:
				burnDamage += damage;

				if (damage > MaxDamage)
				{
					burnDamage = MaxDamage;
				}
				break;

			case DamageType.TOX:
				toxinDamage += damage;

				if (damage > MaxDamage)
				{
					toxinDamage = MaxDamage;
				}
				break;

			case DamageType.OXY:
				suffocationDamage += damage;

				if (damage > MaxDamage)
				{
					suffocationDamage = MaxDamage;
				}
				break;
		}
		UpdateSeverity();
	}

	//Restore/heal damage from here
	public virtual void HealDamage(int damage, DamageType type)
	{
		switch (type)
		{
			case DamageType.BRUTE:
				bruteDamage -= damage;

				if (bruteDamage < 0)
				{
					bruteDamage = 0;
				}
				break;

			case DamageType.BURN:
				burnDamage -= damage;

				if (burnDamage < 0)
				{
					burnDamage = 0;
				}
				break;

			case DamageType.TOX:
				toxinDamage -= damage;

				if (toxinDamage < 0)
				{
					toxinDamage = 0;
				}
				break;

			case DamageType.OXY:
				suffocationDamage -= damage;

				if (suffocationDamage < 0)
				{
					suffocationDamage = 0;
				}
				break;
		}
		UpdateSeverity();
	}

	private void UpdateIcons()
	{
		if (!IsLocalPlayer())
		{
			return;
		}
		UIManager.PlayerHealthUI.SetBodyTypeOverlay(this);
	}

	protected bool IsLocalPlayer()
	{
		//kinda crappy way to determine local player,
		//but otherwise UpdateIcons would have to be moved to HumanHealthBehaviour
		return PlayerManager.LocalPlayerScript == gameObject.GetComponentInParent<PlayerScript>();
	}

	private void UpdateSeverity()
	{
		int damage = CountTotalDamage(); //Gets total damage from all types to determine severity later

		float severity = (float) damage / MaxDamage;
		if (severity >= 0.2 && severity < 0.4)
		{
			Severity = DamageSeverity.Moderate;
		}
		else if (severity >= 0.4 && severity < 0.7)
		{
			Severity = DamageSeverity.Bad;
		}
		else if (severity >= 0.7)
		{
			Severity = DamageSeverity.Critical;
		}
		else
		{
			Severity = DamageSeverity.None;
		}
		UpdateIcons();
	}


	public virtual void RestoreDamage()
	{
		bruteDamage = 0;
		burnDamage = 0;
		toxinDamage = 0;
		bruteDamage = 0;

		UpdateSeverity();
	}

	private int CountTotalDamage()
	{
		return bruteDamage + burnDamage + toxinDamage + suffocationDamage;
	}
}