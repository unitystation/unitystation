using System;
using System.Collections;
using Messages.Server.HealthMessages;
using UnityEngine;
using Mirror;

/// <summary>
///		Health Monitoring component for all Living entities
///     Monitors the state of the entities health on the server and acts accordingly
/// </summary>
public class HealthStateMonitor : ManagedNetworkBehaviour
{
	//Cached members
	float overallHealthCache;
	ConsciousState consciousStateCache;
	bool isSuffocatingCache;
	float temperatureCache;
	float pressureCache;
	int heartRateCache;
	float bloodLevelCache;
	float oxygenDamageCache;
	float toxinLevelCache;
	bool isHuskCache;
	int brainDamageCache;

	private LivingHealthBehaviour livingHealthBehaviour;
	float tickRate = 1f;
	float tick = 0f;
	bool init = false;

	/// ---------------------------
	/// INIT FUNCTIONS
	/// ---------------------------
	void Awake()
	{
		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();
	}

	public override void OnStartServer()
	{
		InitServerCache();
		base.OnStartServer();
	}

	void InitServerCache()
	{
		livingHealthBehaviour.EnsureInit(); //Was getting called before initialisation
		overallHealthCache = livingHealthBehaviour.OverallHealth;
		consciousStateCache = livingHealthBehaviour.ConsciousState;

		if (livingHealthBehaviour.respiratorySystem != null)
		{
			isSuffocatingCache = livingHealthBehaviour.respiratorySystem.IsSuffocating;
			temperatureCache = livingHealthBehaviour.respiratorySystem.temperature;
			pressureCache = livingHealthBehaviour.respiratorySystem.pressure;
		}
		else
		{
			Logger.LogWarning($"No {nameof(livingHealthBehaviour.respiratorySystem)} found on {this}. Is this intended?", Category.Health);
		}

		UpdateBloodCaches();
		if (livingHealthBehaviour.brainSystem != null)
		{
			isHuskCache = livingHealthBehaviour.brainSystem.IsHuskServer;
			brainDamageCache = livingHealthBehaviour.brainSystem.BrainDamageAmt;
		}
		init = true;
	}

	void UpdateBloodCaches()
	{
		heartRateCache = livingHealthBehaviour.bloodSystem.HeartRate;
		bloodLevelCache = livingHealthBehaviour.bloodSystem.BloodLevel;
		oxygenDamageCache = livingHealthBehaviour.bloodSystem.OxygenDamage;
		toxinLevelCache = livingHealthBehaviour.bloodSystem.ToxinLevel;
	}

	/// ---------------------------
	/// SYSTEM MONITOR
	/// ---------------------------
	public override void UpdateMe()
	{
		if (isServer && init)
		{
			MonitorCrucialStats();
			tick += Time.deltaTime;
			if (tick > tickRate)
			{
				tick = 0f;
				MonitorNonCrucialStats();
			}
		}
	}

	// Monitoring stats that need to be updated straight away on client if there is any change
	[Server]
	void MonitorCrucialStats()
	{
		CheckOverallHealth();
		CheckRespiratoryHealth();
		CheckTemperature();
		CheckPressure();
		CheckCruicialBloodHealth();
		CheckConsciousState();
	}

	// Monitoring stats that don't need to be updated straight away on clients
	// (changes are updated at 1 second intervals)
	[Server]
	void MonitorNonCrucialStats()
	{
		CheckNonCrucialBloodHealth();
		if (livingHealthBehaviour.brainSystem != null)
		{
			CheckNonCrucialBrainHealth();
		}
	}

	void CheckConsciousState()
	{
		if (consciousStateCache != livingHealthBehaviour.ConsciousState)
		{
			consciousStateCache = livingHealthBehaviour.ConsciousState;
			SendConsciousUpdate();
		}
	}

	void CheckOverallHealth()
	{
		if (overallHealthCache != livingHealthBehaviour.OverallHealth)
		{
			overallHealthCache = livingHealthBehaviour.OverallHealth;
			SendOverallUpdate();
		}
	}

	void CheckRespiratoryHealth()
	{
		if (isSuffocatingCache != livingHealthBehaviour.respiratorySystem.IsSuffocating)
		{
			isSuffocatingCache = livingHealthBehaviour.respiratorySystem.IsSuffocating;
			SendRespiratoryUpdate();
		}
	}

	void CheckTemperature()
	{
		if (temperatureCache != livingHealthBehaviour.respiratorySystem.temperature)
		{
			temperatureCache = livingHealthBehaviour.respiratorySystem.temperature;
			SendTemperatureUpdate();
		}
	}

	void CheckPressure()
	{
		if (pressureCache != livingHealthBehaviour.respiratorySystem.pressure)
		{
			pressureCache = livingHealthBehaviour.respiratorySystem.pressure;
			SendPressureUpdate();
		}
	}

	void CheckCruicialBloodHealth()
	{
		if (toxinLevelCache != livingHealthBehaviour.bloodSystem.ToxinLevel ||
			heartRateCache != livingHealthBehaviour.bloodSystem.HeartRate)
		{
			UpdateBloodCaches();
			SendBloodUpdate();
		}
	}

	void CheckNonCrucialBloodHealth()
	{
		if (bloodLevelCache != livingHealthBehaviour.bloodSystem.BloodLevel ||
			oxygenDamageCache != livingHealthBehaviour.bloodSystem.OxygenDamage)
		{
			UpdateBloodCaches();
			SendBloodUpdate();
		}
	}

	void CheckNonCrucialBrainHealth()
	{
		if (isHuskCache != livingHealthBehaviour.brainSystem.IsHuskServer ||
			brainDamageCache != livingHealthBehaviour.brainSystem.BrainDamageAmt)
		{
			isHuskCache = livingHealthBehaviour.brainSystem.IsHuskServer;
			brainDamageCache = livingHealthBehaviour.brainSystem.BrainDamageAmt;
			SendBrainUpdate();
		}
	}

	/// ---------------------------
	/// SEND TO ALL SERVER --> CLIENT
	/// ---------------------------

	void SendConsciousUpdate()
	{
		//HealthConsciousMessage.SendToAll(gameObject, livingHealthBehaviour.ConsciousState);
	}

	void SendOverallUpdate()
	{
		//HealthOverallMessage.Send(gameObject, gameObject, livingHealthBehaviour.OverallHealth);
	}

	void SendBloodUpdate()
	{
		HealthBloodMessage.Send(gameObject, gameObject, heartRateCache, bloodLevelCache,
			oxygenDamageCache, toxinLevelCache);
	}

	void SendBrainUpdate()
	{
		if (livingHealthBehaviour.brainSystem != null)
		{
			HealthBrainMessage.SendToAll(gameObject, livingHealthBehaviour.brainSystem.IsHuskServer,
				livingHealthBehaviour.brainSystem.BrainDamageAmt);
		}
	}

	/// ---------------------------
	/// SEND TO INDIVIDUAL CLIENT
	/// ---------------------------

	void SendOverallUpdate(GameObject requestor)
	{
		//HealthOverallMessage.Send(requestor, gameObject, livingHealthBehaviour.OverallHealth);
	}

	void SendConsciousUpdate(GameObject requestor)
	{
		//HealthConsciousMessage.Send(requestor, gameObject, livingHealthBehaviour.ConsciousState);
	}

	void SendBloodUpdate(GameObject requestor)
	{
		HealthBloodMessage.Send(requestor, gameObject, heartRateCache, bloodLevelCache,
			oxygenDamageCache, toxinLevelCache);
	}

	void SendRespiratoryUpdate()
	{
		//Done
		//HealthRespiratoryMessage.Send(gameObject, isSuffocatingCache);
	}

	void SendTemperatureUpdate()
	{
		//HealthTemperatureMessage.Send(gameObject, temperatureCache);
	}

	void SendPressureUpdate()
	{
		//HealthPressureMessage.Send(gameObject, pressureCache);
	}

	void SendBrainUpdate(GameObject requestor)
	{
		if (livingHealthBehaviour.brainSystem != null)
		{
			HealthBrainMessage.Send(requestor, gameObject, livingHealthBehaviour.brainSystem.IsHuskServer,
				livingHealthBehaviour.brainSystem.BrainDamageAmt);
		}
	}

	/// ---------------------------
	/// CLIENT REQUESTS
	/// ---------------------------

	public void ProcessClientUpdateRequest(GameObject requestor)
	{
		StartCoroutine(ControlledClientUpdate(requestor));
	//	Logger.Log("Server received a request for health update from: " + requestor.name + " for: " + gameObject.name);
	}

	/// <summary>
	/// This is mainly used to update new Clients on connect.
	/// So we do not spam too many net messages at once for a direct
	/// client update, control the rate of update slowly:
	/// </summary>
	IEnumerator ControlledClientUpdate(GameObject requestor)
	{
		SendConsciousUpdate(requestor);

		yield return WaitFor.Seconds(.1f);

		SendOverallUpdate(requestor);

		yield return WaitFor.Seconds(.1f);

		SendBloodUpdate(requestor);

		yield return WaitFor.Seconds(.1f);

		SendRespiratoryUpdate();

		yield return WaitFor.Seconds(.1f);

		SendTemperatureUpdate();

		yield return WaitFor.Seconds(.1f);

		SendPressureUpdate();

		yield return WaitFor.Seconds(.1f);

		if (livingHealthBehaviour.brainSystem != null)
		{
			SendBrainUpdate(requestor);
			yield return WaitFor.Seconds(.1f);
		}

		for (int i = 0; i < livingHealthBehaviour.BodyParts.Count; i++)
		{
			HealthBodyPartMessage.Send(requestor, gameObject,
				livingHealthBehaviour.BodyParts[i].Type,
				livingHealthBehaviour.BodyParts[i].BruteDamage,
				livingHealthBehaviour.BodyParts[i].BurnDamage);
			yield return WaitFor.Seconds(.1f);
		}
	}
}