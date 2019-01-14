using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///		Health Monitoring component for all Living entities
///     Monitors the state of the entities health on the server and acts accordingly
/// </summary>
public class HealthStateMonitor : ManagedNetworkBehaviour
{
	//Cached members
	int overallHealthCache;
	ConsciousState consciousStateCache;
	bool isBreathingCache;
	bool isSuffocatingCache;
	int heartRateCache;
	int bloodLevelCache;
	int oxygenLevelCache;
	int toxinLevelCache;
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

	public override void OnStartClient()
	{
		if (!isServer)
		{
			StartCoroutine(ClientWaitForLocal());
		}
		base.OnStartClient();
	}

	IEnumerator ClientWaitForLocal()
	{
		while (PlayerManager.LocalPlayer == null)
		{
			yield return YieldHelper.EndOfFrame;
		}
		yield return YieldHelper.EndOfFrame;
		RequestHealthMessage.Send(gameObject);
	//	Logger.Log("SEND REQUEST TO UPDATE: " + gameObject.name, Category.Health);
	}

	void InitServerCache()
	{
		overallHealthCache = livingHealthBehaviour.OverallHealth;
		consciousStateCache = livingHealthBehaviour.ConsciousState;
		isBreathingCache = livingHealthBehaviour.respiratorySystem.IsBreathing;
		isSuffocatingCache = livingHealthBehaviour.respiratorySystem.IsSuffocating;
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
		oxygenLevelCache = livingHealthBehaviour.bloodSystem.OxygenLevel;
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
		CheckCruicialBloodHealth();
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

	void CheckOverallHealth()
	{
		if (overallHealthCache != livingHealthBehaviour.OverallHealth ||
			consciousStateCache != livingHealthBehaviour.ConsciousState)
		{
			overallHealthCache = livingHealthBehaviour.OverallHealth;
			consciousStateCache = livingHealthBehaviour.ConsciousState;
			SendOverallUpdate();
		}
	}

	void CheckRespiratoryHealth()
	{
		if (isBreathingCache != livingHealthBehaviour.respiratorySystem.IsBreathing ||
			isSuffocatingCache != livingHealthBehaviour.respiratorySystem.IsSuffocating)
		{
			isBreathingCache = livingHealthBehaviour.respiratorySystem.IsBreathing;
			isSuffocatingCache = livingHealthBehaviour.respiratorySystem.IsSuffocating;
			SendRespiratoryUpdate();
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
			oxygenLevelCache != livingHealthBehaviour.bloodSystem.OxygenLevel)
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

	void SendOverallUpdate()
	{
		HealthOverallMessage.SendToAll(gameObject, livingHealthBehaviour.OverallHealth,
			livingHealthBehaviour.ConsciousState);
	}

	void SendBloodUpdate()
	{
		HealthBloodMessage.SendToAll(gameObject, heartRateCache, bloodLevelCache,
			oxygenLevelCache, toxinLevelCache);
	}

	void SendRespiratoryUpdate()
	{
		HealthRespiratoryMessage.SendToAll(gameObject, livingHealthBehaviour.respiratorySystem.IsBreathing,
			livingHealthBehaviour.respiratorySystem.IsSuffocating);
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
		HealthOverallMessage.Send(requestor, gameObject, livingHealthBehaviour.OverallHealth,
			livingHealthBehaviour.ConsciousState);
	}

	void SendBloodUpdate(GameObject requestor)
	{
		HealthBloodMessage.Send(requestor, gameObject, heartRateCache, bloodLevelCache,
			oxygenLevelCache, toxinLevelCache);
	}

	void SendRespiratoryUpdate(GameObject requestor)
	{
		HealthRespiratoryMessage.Send(requestor, gameObject, livingHealthBehaviour.respiratorySystem.IsBreathing,
			livingHealthBehaviour.respiratorySystem.IsSuffocating);
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
		SendOverallUpdate(requestor);

		yield return YieldHelper.DeciSecond;

		SendBloodUpdate(requestor);

		yield return YieldHelper.DeciSecond;

		SendRespiratoryUpdate(requestor);

		yield return YieldHelper.DeciSecond;

		if (livingHealthBehaviour.brainSystem != null)
		{
			SendBrainUpdate(requestor);
			yield return YieldHelper.DeciSecond;
		}

		for (int i = 0; i < livingHealthBehaviour.BodyParts.Count; i++)
		{
			HealthBodyPartMessage.Send(requestor, gameObject,
				livingHealthBehaviour.BodyParts[i].Type,
				livingHealthBehaviour.BodyParts[i].BruteDamage,
				livingHealthBehaviour.BodyParts[i].BurnDamage);
			yield return YieldHelper.DeciSecond;
		}
	}
}