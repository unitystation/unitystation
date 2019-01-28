using Atmospherics;
using UnityEngine;

/// <summary>
/// Controls the RepiratorySystem for this living thing
/// Mostly managed server side and states sent to the clients
/// </summary>
public class RespiratorySystem : MonoBehaviour //Do not turn into NetBehaviour
{
	public bool IsBreathing { get; private set; } = true;
	public bool IsSuffocating { get; private set; }

	/// <summary>
	/// 2 minutes of suffocation = 100% damage
	/// </summary>
	public int SuffocationDamage => Mathf.RoundToInt((suffocationTime / 120f) * 100f);

	public float suffocationTime = 0f;

	private BloodSystem bloodSystem;
	private LivingHealthBehaviour livingHealthBehaviour;
	private PlayerScript playerScript;

	private float tickRate = 1f;
	private float tick = 0f;

	void Awake()
	{
		bloodSystem = GetComponent<BloodSystem>();
		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();
		playerScript = GetComponent<PlayerScript>();
	}

	void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
	}

	void OnDisable()
	{
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	//Handle by UpdateManager
	void UpdateMe()
	{
		//Server Only:
		if (CustomNetworkManager.Instance._isServer)
		{
			tick += Time.deltaTime;
			if (tick >= tickRate)
			{
				tick = 0f;
				MonitorSystem();
			}

			if (IsSuffocating)
			{
				CheckSuffocation();
			}
		}
	}

	private void MonitorSystem()
	{
		if (livingHealthBehaviour.IsDead)
		{
			return;
		}

		CheckBreathing();
	}

	/// Check breathing state
	private void CheckBreathing()
	{
		// Try not to make super long conditions here, break them up
		// into each individual condition for ease of reading
		if (IsBreathing)
		{
			MonitorAirInput();

			//Conditions that would stop breathing:
			if (livingHealthBehaviour.OverallHealth <= 0)
			{
				IsBreathing = false;
				IsSuffocating = true;
			}

//			if (IsInSpace() && !IsEvaCompatible())
//			{
//				IsBreathing = false;
//				IsSuffocating = true;
//			}

			//TODO: other conditions that would prevent breathing
		}

		if (!IsBreathing)
		{
			if (livingHealthBehaviour.OverallHealth > 0)
			{
//				if (IsInSpace() && IsEvaCompatible())
//				{
//					IsBreathing = true;
//					GetComponent<PlayerNetworkActions>().SetConsciousState(true);
//				}
//
//				if (!IsInSpace())
//				{
//					IsBreathing = true;
//					GetComponent<PlayerNetworkActions>().SetConsciousState(true);
//				}
			}
		}
	}

	private void MonitorAirInput()
	{
		MetaDataNode node = MatrixManager.GetMetaDataAt(transform.position.RoundToInt());

		CheckPressureDamage(node.Atmos.Pressure);

		CheckBreath(node);


//		if(node)
		//TODO Finish when atmos is implemented. Basically deliver any elements to the
		//the blood stream every breath
		//Check atmos values for the tile you are on

		//FIXME remove when above TODO is done:
//		if (!IsInSpace() || IsInSpace() && IsEvaCompatible())
//		{
//			//Delivers oxygen to the blood from a single breath
//			bloodSystem.OxygenLevel += 30;
//		}
	}

	private void CheckBreath(MetaDataNode node)
	{
		GasMix breathGasMix = node.Atmos.RemoveVolume(AtmosConstants.BREATH_VOLUME);

		float oxygenPressure = node.Atmos.GetPressure(Gas.Oxygen);

		float oxygenUsed = 0;

		float oxygenSafeMin = 16;

		if (oxygenPressure < oxygenSafeMin)
		{
			// TODO gasp with 20 % prob

			if (oxygenPressure > 0)
			{
				float ratio = 1 - oxygenPressure / oxygenSafeMin;

				ApplyDamage(Mathf.Min(5 * ratio, 3), DamageType.Oxy);
				oxygenUsed = breathGasMix.GetMoles(Gas.Oxygen) * ratio;
			}
			else
			{
				ApplyDamage(3, DamageType.Oxy);
			}
		}
		else
		{
			oxygenUsed = breathGasMix.GetMoles(Gas.Oxygen);

			bloodSystem.OxygenLevel += 30;
		}

		if (oxygenUsed > 0)
		{
			breathGasMix.RemoveGas(Gas.Oxygen, oxygenUsed);
			breathGasMix.AddGas(Gas.CarbonDioxide, oxygenUsed);
		}

		node.Atmos += breathGasMix;
	}

	private void CheckPressureDamage(float pressure)
	{
		if (pressure < AtmosConstants.MINIMUM_OXYGEN_PRESSURE)
		{
			ApplyDamage(AtmosConstants.LOW_PRESSURE_DAMAGE, DamageType.Brute);
		}
		else if (pressure > AtmosConstants.HAZARD_HIGH_PRESSURE)
		{
			float damage = Mathf.Min(((pressure / AtmosConstants.HAZARD_HIGH_PRESSURE) - 1) * AtmosConstants.PRESSURE_DAMAGE_COEFFICIENT,
				AtmosConstants.MAX_HIGH_PRESSURE_DAMAGE);

			ApplyDamage(damage, DamageType.Brute);
		}
	}

	private void ApplyDamage(float amount, DamageType damageType)
	{
		livingHealthBehaviour.ApplyDamage(null, amount,damageType);
	}

	/// Preform any suffocation monitoring here:
	private void CheckSuffocation()
	{
		if (IsBreathing)
		{
			IsSuffocating = false;
			suffocationTime = 0f;
		}
		else
		{
			suffocationTime += Time.deltaTime;
		}
	}

	private bool IsEvaCompatible()
	{
		if (playerScript == null)
		{
			Logger.Log("This is not a human player. Develop a way to detect EVA equipment on animals",
				Category.Health);
			return false;
		}

		GameObject headItem = playerScript.playerNetworkActions.Inventory["head"].Item;
		GameObject suitItem = playerScript.playerNetworkActions.Inventory["suit"].Item;
		if (headItem == null || suitItem == null)
		{
			return false;
		}

		ItemAttributes headItemAtt = headItem.GetComponent<ItemAttributes>();
		ItemAttributes suitItemAtt = suitItem.GetComponent<ItemAttributes>();

		// TODO when atmos is merged and oxy tanks are in, then check oxygen flow
		// through mask here

		if (headItemAtt == null || suitItemAtt == null)
		{
			return false;
		}

		return headItemAtt.evaCapable && suitItemAtt.evaCapable;
	}

	// --------------------
	// UPDATES FROM SERVER
	// --------------------

	/// <summary>
	/// Updated from server via NetMsg
	/// </summary>
	public void UpdateClientRespiratoryStats(bool isBreathing, bool isSuffocating)
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		IsBreathing = isBreathing;
		IsSuffocating = isSuffocating;
	}
}