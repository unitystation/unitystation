using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the blood system for a Living Entity
/// Only really updated on the Server!
/// Do not derive this class from NetworkBehaviour
/// </summary>
public class BloodSystem : MonoBehaviour
{
	/// <summary>
	/// How much toxin is found in the blood. 0% to 100%
	/// </summary>
	public int ToxinLevel
	{
		get { return Mathf.Clamp(toxinLevel, 0, 101); }
		set { toxinLevel = Mathf.Clamp(value, 0, 101); }
	}

	/// <summary>
	/// Oxygen levels found in the blood. 0% to 100%
	/// </summary>
	public int OxygenLevel
	{
		get { return Mathf.Clamp(oxygenLevel, 0, 101); }
		set { oxygenLevel = Mathf.Clamp(value, 0, 101); }
	}

	private int oxygenLevel = 100;
	private int toxinLevel = 0;
	private LivingHealthBehaviour livingHealthBehaviour;
	private DNAandBloodType bloodType;
	private readonly float bleedRate = 2f;
	private int bleedVolume;
	public int BloodLevel = (int)BloodVolume.NORMAL;
	public bool IsBleeding { get; private set; }

	void Awake()
	{
		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();
	}

	void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
	}

	void OnDisable()
	{
		if(UpdateManager.Instance != null)
		UpdateManager.Instance.Remove(UpdateMe);
	}

	//Initial setting for blood type. Server only
	public void SetBloodType(DNAandBloodType dnaBloodType)
	{
		bloodType = dnaBloodType;
	}

	//Handle by UpdateManager
	void UpdateMe()
	{

	}

	/// <summary>
	/// Subtract an amount of blood from the player. Server Only
	/// </summary>
	public void AddBloodLoss(int amount)
	{
		if (amount <= 0)
		{
			return;
		}
		bleedVolume += amount;
		TryBleed();
	}

	private void TryBleed()
	{
		//don't start another coroutine when already bleeding
		if (!IsBleeding)
		{
			IsBleeding = true;
			StartCoroutine(StartBleeding());
		}
	}

	private IEnumerator StartBleeding()
	{
		while (IsBleeding)
		{
			LoseBlood(bleedVolume);

			yield return new WaitForSeconds(bleedRate);
		}
	}

	/// <summary>
	/// Stems any bleeding. Server Only.
	/// </summary>
	public void StopBleeding()
	{
		bleedVolume = 0;
		IsBleeding = false;
	}

	private void LoseBlood(int amount)
	{
		if (amount <= 0)
		{
			return;
		}
		Logger.LogTraceFormat("Lost blood: {0}->{1}", Category.Health, BloodLevel, BloodLevel - amount);
		BloodLevel -= amount;
		BloodSplatSize scaleOfTragedy;
		if (amount > 0 && amount < 15)
		{
			scaleOfTragedy = BloodSplatSize.small;
		}
		else if (amount >= 15 && amount < 40)
		{
			scaleOfTragedy = BloodSplatSize.medium;
		}
		else
		{
			scaleOfTragedy = BloodSplatSize.large;
		}

		EffectsFactory.Instance.BloodSplat(transform.position, scaleOfTragedy);
	}

	/// <summary>
	/// Restore blood level
	/// </summary>
	private void RestoreBlood()
	{
		BloodLevel = (int)BloodVolume.NORMAL;
	}

	private static float BleedFactor(DamageType damageType)
	{
		float random = Random.Range(-0.2f, 0.2f);
		switch (damageType)
		{
			case DamageType.Brute:
				return 0.6f + random;
			case DamageType.Burn:
				return 0.4f + random;
			case DamageType.Tox:
				return 0.2f + random;
		}
		return 0;
	}

	/// <summary>
	/// Determine if there is any blood damage (toxin, oxygen loss) or bleeding that needs to occur
	/// Server only!
	/// </summary>
	public void AffectBloodState(BodyPartType bodyPartType, DamageType damageType, int amount, bool isHeal = false)
	{
		BodyPartBehaviour bodyPart = livingHealthBehaviour.FindBodyPart(bodyPartType);

		if (isHeal)
		{
			CheckHealing(bodyPart, damageType, amount);
			return;
		}

		//Check if limb should start bleeding (Bleeding is only for Players, not animals)
		if (damageType == DamageType.Brute && !IsBleeding)
		{
			// don't start bleeding if limb is in ok condition after it received damage
			switch (bodyPart.Severity)
			{
				case DamageSeverity.Moderate:
				case DamageSeverity.Bad:
				case DamageSeverity.Critical:
					int bloodLoss = (int)(amount * BleedFactor(damageType));
					LoseBlood(bloodLoss);
					AddBloodLoss(bloodLoss);
					break;
			}
		}

		if (damageType == DamageType.Tox)
		{
			ToxinLevel += amount;
		}
	}

	//Do any healing stuff:
	private void CheckHealing(BodyPartBehaviour bodyPart, DamageType damageType, int healAmt)
	{
		Debug.Log("TODO PRIORITY: Do Blood Healing!!");
	}
}