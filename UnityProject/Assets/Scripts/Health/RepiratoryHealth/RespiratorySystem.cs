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
	//TODO: Remove GasTypes when atmos is merged and use the
	//enums defined for atmos instead
	public enum GasTypes
	{
		None, // = vacumm
		Air, // = 80% nitrogen + 20% oxygen
		//only need these values for time being until atmos is merged then
		//we can remove this and use real atmos element percentages 
	}
	private BloodSystem bloodSystem;
	private LivingHealthBehaviour livingHealthBehaviour;
	private PlayerScript playerScript;
	public bool IsBreathing { get; private set; } = true;
	public bool IsSuffocating { get; private set; } = false;
	private float tickRate = 1f;
	private float tick = 0f;
	private float suffocationTime = 0f;

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
				MonitorSystem();
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
		CheckSuffocation();
	}

	/// Check breathing state
	void CheckBreathing()
	{
		// Try not to make super long conditions here, break them up
		// into each individual condition for ease of reading
		if (IsBreathing)
		{
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
			if (IsInSpace() && IsEvaCompatible())
			{
				IsBreathing = true;
			}

			if (!IsInSpace())
			{
				IsBreathing = true;
			}
		}
	}

	/// Preform any suffocation monitoring here:
	void CheckSuffocation()
	{
		if (IsSuffocating)
		{
			if (IsBreathing)
			{
				IsSuffocating = false;
				suffocationTime = 0f;
			}
			else
			{
				suffocationTime += Time.deltaTime;
				if (suffocationTime > 240f) //4 minutes without oxygen
				{
					//4 minutes of no breath = death
					livingHealthBehaviour.Death();
				}
			}
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
}