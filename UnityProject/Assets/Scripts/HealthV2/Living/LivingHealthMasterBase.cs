using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;

public abstract class LivingHealthMasterBase : NetworkBehaviour
{
	/// <summary>
	/// Server side, each mob has a different one and never it never changes
	/// </summary>
	public int mobID { get; private set; }

	private DNAandBloodType DNABloodType;

	// JSON string for blood types and DNA.
	[SyncVar(hook = nameof(DNASync))] //May remove this in the future and only provide DNA info on request
	private string DNABloodTypeJSON;

	private float tickRate = 1f;
	private float tick = 0;

	private RegisterTile registerTile;

	[NonSerialized] public ConsciousStateEvent OnConsciousStateChangeServer = new ConsciousStateEvent();
	public ConsciousState ConsciousState
	{
		get => consciousState;
		protected set
		{
			ConsciousState oldState = consciousState;
			if (value != oldState)
			{
				consciousState = value;
				if (isServer)
				{
					OnConsciousStateChangeServer.Invoke(oldState, value);
				}
			}
		}
	}
	private ConsciousState consciousState;

	[SerializeField]
	private float maxHealth = 100;

	private float overallHealth = 100;
	public float OverallHealth => overallHealth;

	[CanBeNull]
	private CirculatorySystemBase circulatorySystem;

	[CanBeNull]
	private RespiratorySystemBase respiratorySystem;

	private bool isDead = false;
	public bool IsDead => isDead;

	public bool IsCrit => ConsciousState == ConsciousState.UNCONSCIOUS;
	public bool IsSoftCrit => ConsciousState == ConsciousState.BARELY_CONSCIOUS;

	public bool InCardiacArrest => circulatorySystem != null && circulatorySystem.HeartIsStopped;


	public virtual void Awake()
	{
		EnsureInit();
	}

	void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		UpdateManager.Add(PeriodicUpdate, 1f);
	}

	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
	}

	private void EnsureInit()
	{
		if (registerTile != null) return;
		registerTile = GetComponent<RegisterTile>();
		//Always include blood for living entities:
	}

	public override void OnStartServer()
	{
		EnsureInit();
		mobID = PlayerManager.Instance.GetMobID();

		//Generate BloodType and DNA
		DNABloodType = new DNAandBloodType();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		EnsureInit();
		StartCoroutine(WaitForClientLoad());
	}

	private void UpdateMe()
	{
	}

	private void PeriodicUpdate()
	{
	}

	IEnumerator WaitForClientLoad()
	{
		//wait for DNA:
		while (string.IsNullOrEmpty(DNABloodTypeJSON))
		{
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.EndOfFrame;
		DNASync(DNABloodTypeJSON, DNABloodTypeJSON);
	}

	// This is the DNA SyncVar hook
	private void DNASync(string oldDNA, string updatedDNA)
	{
		EnsureInit();
		DNABloodTypeJSON = updatedDNA;
		DNABloodType = JsonUtility.FromJson<DNAandBloodType>(updatedDNA);
	}
}
