using UnityEngine;

public class BodyPartBehaviour : MonoBehaviour
{
	//Different types of damages for medical.
	private int bruteDamage;
	private int burnDamage;
	public int BruteDamage { get { return bruteDamage; } set { bruteDamage = Mathf.Clamp(value, 0, 101); } }
	public int BurnDamage { get { return burnDamage; } set { burnDamage = Mathf.Clamp(value, 0, 101); } }

	public Sprite GrayDamageMonitorIcon;

	public Sprite GreenDamageMonitorIcon;

	private int MaxDamage = 100;

	public Sprite OrangeDamageMonitorIcon;
	public Sprite RedDamageMonitorIcon;
	public BodyPartType Type;
	public Sprite YellowDamageMonitorIcon;

	public DamageSeverity Severity; //{ get; private set; }
	public int OverallDamage { get { return BruteDamage + BurnDamage; } }

	void Start()
	{
		UpdateIcons();
	}

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
			case DamageType.Brute:
				BruteDamage += damage;
				break;

			case DamageType.Burn:
				BurnDamage += damage;
				break;
		}
		UpdateSeverity();
	}

	//Restore/heal damage from here
	public virtual void HealDamage(int damage, DamageType type)
	{
		switch (type)
		{
			case DamageType.Brute:
				BruteDamage -= damage;
				break;

			case DamageType.Burn:
				BurnDamage -= damage;
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
		float severity = (float)OverallDamage / MaxDamage;
		if (severity < 0.2)
		{
			Severity = DamageSeverity.None;
		}
		else
		if (severity >= 0.2 && severity < 0.4)
		{
			Severity = DamageSeverity.Moderate;
		}
		else if (severity >= 0.4 && severity < 0.7)
		{
			Severity = DamageSeverity.Bad;
		}
		else if (severity >= 0.7 && severity < 1f)
		{
			Severity = DamageSeverity.Critical;
		}
		else if (severity >= 1f)
		{
			Severity = DamageSeverity.Max;
		}

		UpdateIcons();
	}

	public virtual void RestoreDamage()
	{
		bruteDamage = 0;
		burnDamage = 0;
		UpdateSeverity();
	}

	// --------------------
	// UPDATES FROM SERVER
	// -------------------- 
	public void UpdateClientBodyPartStat(int _bruteDamage, int _burnDamage)
	{
		bruteDamage = _bruteDamage;
		burnDamage = _burnDamage;
		UpdateSeverity();
	}
}