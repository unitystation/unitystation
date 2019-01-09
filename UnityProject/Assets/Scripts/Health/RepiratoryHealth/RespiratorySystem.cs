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
	public bool IsBreathing { get; private set; } = true;
	private float tickRate = 1f;
	private float tick = 0f;

	void Awake()
	{
		bloodSystem = GetComponent<BloodSystem>();
		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();
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
		tick += Time.deltaTime;
		if (tick >= tickRate)
		{
			MonitorSystem();
		}
	}

	void MonitorSystem()
	{
		
	}

}