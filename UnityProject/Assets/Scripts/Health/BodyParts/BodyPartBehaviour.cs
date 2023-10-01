using System;
using System.Text;
using Logs;
using UnityEngine;

public class BodyPartBehaviour : MonoBehaviour
{
	//Different types of damages for medical.
	private float bruteDamage;
	private float burnDamage;
	public float BruteDamage
	{
		get => bruteDamage;
		private set => bruteDamage = Mathf.Clamp(value, 0, MaxDamage);
	}
	public float BurnDamage
	{
		get => burnDamage;
		private set => burnDamage = Mathf.Clamp(value, 0, MaxDamage);
	}

	public LivingHealthBehaviour livingHealthBehaviour;
	public BodyPartType Type;
	public bool isBleeding = false;

	private int MaxDamage;

	public DamageSeverity Severity; //{ get; private set; }
	public float OverallDamage => BruteDamage + BurnDamage;
	public Armor armor = new Armor();

	private int brutePercentage => Mathf.RoundToInt(bruteDamage / MaxDamage * 100);
	private int burnPercentage => Mathf.RoundToInt(burnDamage / MaxDamage * 100);

	void Start()
	{
		UpdateIcons();
	}

	private void Awake()
	{
		//FIXME this is a bad patch for a bad problem. I don't know what's going on with the calculation behind curtains
		// but if bodyparts have less maxDmg than maxHealth, then mobs never die.
		MaxDamage = (int) (livingHealthBehaviour is null ? 99999 : livingHealthBehaviour.maxHealth * 2);
	}

	//Apply damages from here.
	public virtual void ReceiveDamage(DamageType damageType, float damage)
	{
		UpdateDamage(damage, damageType);
		Loggy.LogTraceFormat("{0} received {1} {2} damage. Total {3}/{4}, limb condition is {5}", Category.Damage, Type, damage, damageType, damage, MaxDamage, Severity);
	}

	private void UpdateDamage(float damage, DamageType type)
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

	public float GetDamageValue(DamageType damageType){
		if(damageType == DamageType.Brute)
		{
			return BruteDamage;
		}
		if (damageType == DamageType.Burn)
		{
			return BurnDamage;
		}
		return 0;
	}

	private void UpdateIcons()
	{
		if (!IsLocalPlayer())
		{
			return;
		}
		//UIManager.PlayerHealthUI.SetBodyTypeOverlay(this);
	}

	protected bool IsLocalPlayer()
	{
		//kinda crappy way to determine local player,
		//but otherwise UpdateIcons would have to be moved to HumanHealthBehaviour
		return PlayerManager.LocalPlayerScript == gameObject.GetComponentInParent<PlayerScript>();
	}

	private void UpdateSeverity()
	{
		// update UI limbs depending on their severity of damage
		float severity = OverallDamage / MaxDamage;
		// If the limb is uninjured
		if (severity <= 0)
		{
			Severity = DamageSeverity.None;
		}
		// If the limb is under 10% damage
		else if (severity < 0.1)
		{
			Severity = DamageSeverity.Light;
		}
		// If the limb is under 25% damage
		else if (severity < 0.25)
		{
			Severity = DamageSeverity.LightModerate;
		}
		// If the limb is under 45% damage
		else if (severity < 0.45)
		{
			Severity = DamageSeverity.Moderate;
		}
		// If the limb is under 85% damage
		else if (severity < 0.85)
		{
			Severity = DamageSeverity.Bad;
		}
		// If the limb is under 95% damage
		else if (severity < 0.95f)
		{
			Severity = DamageSeverity.Critical;
		}
		// If the limb is 95% damage or over
		else if (severity >= 0.95f)
		{
			Severity = DamageSeverity.Max;
		}

		UpdateIcons();
	}

	public virtual void RestoreDamage()
	{
		BruteDamage = 0;
		BurnDamage = 0;
		UpdateSeverity();
	}

	// --------------------
	// UPDATES FROM SERVER
	// --------------------
	public void UpdateClientBodyPartStat(float _bruteDamage, float _burnDamage)
	{
		BruteDamage = _bruteDamage;
		BurnDamage = _burnDamage;
		UpdateSeverity();
	}

	public string GetDamageDescription()
	{
		if (OverallDamage.IsBetween(0f, 10f))
		{
			return string.Empty;
		}

		var description = new StringBuilder();
		description.Append(SeverityDescription);
		description.Append(BruteDamageDescription);

		if (brutePercentage > 10 && burnPercentage > 9)
		{
			description.Append(" and ");
		}

		description.Append(BurnDamageDescription);

		return description.ToString();
	}

	private string BruteDamageDescription
	{
		get
		{
			switch (brutePercentage)
			{
				case int n when n.IsBetween(10, 20):
					return "it has some negligible bruises";
				case int n when n.IsBetween(21, 40):
					return "it has minor bruises";
				case int n when n.IsBetween(41, 60):
					return "it has moderate bruises";
				case int n when n.IsBetween(61, 80):
					return "it has heavy bruises";
				case int n when n.IsBetween(81, 100):
					return "it looks ready to fall apart";
				default:
					return ".";
			}
		}
	}

	private string BurnDamageDescription
	{
		get
		{
			switch (burnPercentage)
			{
				case int n when n.IsBetween(10, 20):
					return "it has some negligible burns.";
				case int n when n.IsBetween(21, 40):
					return "it has minor burns.";
				case int n when n.IsBetween(41, 60):
					return "it has moderate burns.";
				case int n when n.IsBetween(61, 80):
					return "it has heavy burns.";
				case int n when n.IsBetween(81, 100):
					return "it looks charred and about to crumble.";
				default:
					return ".";
			}
		}
	}


	private string SeverityDescription
	{
		get
		{
			switch (Severity)
			{
				case DamageSeverity.Light:
				case DamageSeverity.LightModerate:
					return "lightly damaged, ";

				case DamageSeverity.Moderate:
					return "moderately damaged, ";

				case DamageSeverity.Bad:
					return "badly damaged, ";

				case DamageSeverity.Critical:
					return "critically damaged, ";
				case DamageSeverity.Max:
					return "totally destroyed, ";
				default:
					return string.Empty;
			}
		}
	}
}