using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///		Health Monitoring component for all Living entities
///     Monitors the state of the entities health on the server and acts accordingly
/// </summary>
public class HealthStateMonitor : ManagedNetworkBehaviour
{
	private LivingHealthBehaviour LivingHealthBehaviour;
	private int bloodLevelCache;
	private float BloodPercentage = 100f;

	private bool hasStoppedBreathing = false;
	private float breathingDamagedRate = 2f; //dmg every 2 seconds
	private float stoppedBreathingCount = 0f;

	//server only caches
	private int healthServerCache;
	//TODO if client disconnects and reconnects then the clients UI needs to 
	//poll this component and request updated values from the server to set
	//the current state of the UI overlays and hud

	private LivingHealthBehaviour healthBehaviour;

	void Awake()
	{
		healthBehaviour = GetComponent<LivingHealthBehaviour>();
	}

	protected override void OnEnable()
	{
		//Do not call base method for this OnEnable.
	}

	public override void OnStartServer()
	{
		if (isServer)
		{
			healthServerCache = healthBehaviour.OverallHealth;
			bloodLevelCache = healthBehaviour.bloodSystem.BloodLevel;
			UpdateManager.Instance.Add(this);

			if (!healthBehaviour.isNotPlayer)
			{
				StartCoroutine(WaitForLoad());
			}
		}
		base.OnStartServer();
	}

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(1f); //1000ms wait for lag

		UpdateClientUI(100); //Set the UI for this player to 100 percent
	}

	private void OnDestroy()
	{
		if (isServer)
		{
			UpdateManager.Instance.Remove(this);
		}
	}

	//This only runs on the server, server will do the calculations and send
	//messages to the client when needed (or requested)
	public override void UpdateMe()
	{
		ServerMonitorHealth();
		base.UpdateMe();
	}

	private void ServerMonitorHealth()
	{
		//Add other damage methods here like burning, 
		//suffication, etc

		//If already dead then do not check the status of the body anymore
		if (healthBehaviour.IsDead)
		{
			return;
		}

		//Blood calcs:
		if (bloodLevelCache != healthBehaviour.bloodSystem.BloodLevel)
		{
			bloodLevelCache = healthBehaviour.bloodSystem.BloodLevel;
			if (healthBehaviour.bloodSystem.BloodLevel >= 560)
			{
				//Full blood (or more)
				BloodPercentage = 100f;
			}
			else
			{
				BloodPercentage = healthBehaviour.bloodSystem.BloodLevel / 560f * 100f;
			}
		}

		//If blood level falls below health level, then set the health level
		//manually and update the clients UI
		if (BloodPercentage < healthBehaviour.OverallHealth)
		{
			healthServerCache = (int)BloodPercentage;
			healthBehaviour.ServerOnlySetHealth(healthServerCache);
			UpdateClientUI(healthServerCache);
		}

		//Player has stopped breathing:
		if (hasStoppedBreathing && healthBehaviour.OverallHealth > -1f)
		{
			stoppedBreathingCount += Time.deltaTime;
			if (stoppedBreathingCount > breathingDamagedRate)
			{
				stoppedBreathingCount = 0f;

				healthBehaviour.ServerOnlySetHealth(healthServerCache -= 3);
			}
		}

		if (healthBehaviour.OverallHealth != healthServerCache)
		{
			healthServerCache = healthBehaviour.OverallHealth;
			UpdateClientUI(healthServerCache);
		}

		if (healthBehaviour.OverallHealth < 30 && !hasStoppedBreathing)
		{
			hasStoppedBreathing = true;
		}
		else if (healthBehaviour.OverallHealth >= 30 && hasStoppedBreathing)
		{
			hasStoppedBreathing = false;
		}
	}

	//Sends msg to the owner of this player to update their UI
	[Server]
	private void UpdateClientUI(int newHealth)
	{
		UpdateUIMessage.SendHealth(gameObject, newHealth);
	}
}