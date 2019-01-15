using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the RepiratorySystem for this living thing
/// Mostly managed server side and states sent to the clients
/// </summary>
public class RespiratorySystem : MonoBehaviour //Do not turn into NetBehaviour
{
	private BloodSystem bloodSystem;
	private LivingHealthBehaviour livingHealthBehaviour;
	private PlayerScript playerScript;
	public bool IsBreathing { get; private set; } = true;
	public bool IsSuffocating { get; private set; } = false;

	/// <summary>
	/// 2 minutes of suffocation = 100% damage
	/// </summary>
	public int SuffocationDamage { get { return Mathf.RoundToInt((suffocationTime / 120f) * 100f); } }
	private float tickRate = 1f;
	private float tick = 0f;
	public float suffocationTime = 0f;

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
			UpdateManager.Instance.Remove(UpdateMe);
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

	void MonitorSystem()
	{
		if (livingHealthBehaviour.IsDead)
		{
			return;
		}

		CheckBreathing();
	}

	/// Check breathing state
	void CheckBreathing()
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

			if (IsInSpace() && !IsEvaCompatible())
			{
				IsBreathing = false;
				IsSuffocating = true;
			}
			//TODO: other conditions that would prevent breathing
		}

		if (!IsBreathing)
		{
			if (livingHealthBehaviour.OverallHealth > 0)
			{
				if (IsInSpace() && IsEvaCompatible())
				{
					IsBreathing = true;
					GetComponent<PlayerNetworkActions>().SetConsciousState(true);
				}

				if (!IsInSpace())
				{
					IsBreathing = true;
					GetComponent<PlayerNetworkActions>().SetConsciousState(true);
				}
			}
		}
	}

	void MonitorAirInput()
	{
		//TODO Finish when atmos is implemented. Basically deliver any elements to the
		//the blood stream every breath
		//Check atmos values for the tile you are on

		//FIXME remove when above TODO is done:
		if (!IsInSpace() || IsInSpace() && IsEvaCompatible())
		{
			//Delivers oxygen to the blood from a single breath
			bloodSystem.OxygenLevel += 30;
		}
	}

	/// Preform any suffocation monitoring here:
	void CheckSuffocation()
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

	/// TODO: Replace this when atmos pressure is implemented
	/// we are just doing a space check for the time being
	private bool IsInSpace()
	{
		if (MatrixManager.IsSpaceAt(Vector3Int.RoundToInt(transform.position)))
		{
			return true;
		}
		return false;
	}

	private bool IsEvaCompatible()
	{
		if (playerScript == null)
		{
			Logger.Log("This is not a human player. Develop a way to detect EVA equipment on animals",
				Category.Health);
			return false;
		}

		var headItem = playerScript.playerNetworkActions.Inventory["head"].Item;
		var suitItem = playerScript.playerNetworkActions.Inventory["suit"].Item;
		if (headItem == null || suitItem == null)
		{
			return false;
		}

		var headItemAtt = headItem?.GetComponent<ItemAttributes>();
		var suitItemAtt = suitItem?.GetComponent<ItemAttributes>();

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