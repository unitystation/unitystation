using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the Brain System for this living entity
/// Updated Server Side and state is sent to clients
/// Holds the brain for this entity
/// </summary>
public class BrainSystem : MonoBehaviour //Do not turn into NetBehaviour
{
	//The brain! Only used on the server
	private Brain brain;
	private BloodSystem bloodSystem;
	private RespiratorySystem respiratorySystem;
	private LivingHealthBehaviour livingHealthBehaviour;
	private PlayerScript playerScript; //null if it is an animal
	/// <summary>
	/// Is this body just a husk (missing brain)
	/// </summary>
	public bool IsHuskServer => brain == null;
	public bool IsHuskClient { get; private set; }

	/// <summary>
	/// How damaged is the brain
	/// </summary>
	/// <returns>Percentage between 0% and 100%.
	/// -1 means there is no brain present</returns>
	public int BrainDamageAmt { get { if (brain == null) { return -1; } return Mathf.Clamp(brain.BrainDamage, 0, 101); } }
	public int BrainDamageAmtClient { get; private set; }

	private float tickRate = 1f;
	private bool init = false;

	void Start()
	{
		InitSystem();
	}

	void InitSystem()
	{
		playerScript = GetComponent<PlayerScript>();
		bloodSystem = GetComponent<BloodSystem>();
		respiratorySystem = GetComponent<RespiratorySystem>();
		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();

		//Server only
		if (CustomNetworkManager.Instance._isServer)
		{
			//Spawn a brain and connect the brain to this living entity
			brain = new Brain();
			brain.ConnectBrainToBody(gameObject);
			if (playerScript != null)
			{
				//TODO: See https://github.com/unitystation/unitystation/issues/1429
			}
			init = true;
		}
	}

	void OnEnable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Add(ServerPeriodicUpdate, tickRate);
		}
	}

	void OnDisable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerPeriodicUpdate);
		}
	}

	// Controlled via UpdateManager
	void ServerPeriodicUpdate()
	{
		if (!init)
		{
			return;
		}

		checkOverallDamage();
	}


	void checkOverallDamage(){
		if(bloodSystem.OxygenDamage > 200){
			if (!livingHealthBehaviour.IsDead)
			{
				livingHealthBehaviour.Death();
			}
		}
	}

	// --------------------
	// UPDATES FROM SERVER
	// --------------------

	/// <summary>
	/// Updated via server NetMsg
	/// </summary>
	public void UpdateClientBrainStats(bool isHusk, int brainDmgAmt)
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			return;
		}
		IsHuskClient = isHusk;
		BrainDamageAmtClient = brainDmgAmt;
	}
}
