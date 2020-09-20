using System;
using System.Collections;
using System.Collections.Generic;
using Health.Sickness;
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

	//I don't know what this is. It was in the first Health system.
	public float RTT;

	[SerializeField]
	private float maxHealth = 100;

	private float overallHealth = 100;
	public float OverallHealth => overallHealth;

	[SerializeField]
	[Tooltip("These are the things that will hold all our organs and implants.")]
	private List<BodyPartContainerBase> bodyPartContainers;

	[CanBeNull]
	private CirculatorySystemBase circulatorySystem;
	public CirculatorySystemBase CirculatorySystem => circulatorySystem;

	[CanBeNull]
	private RespiratorySystemBase respiratorySystem;
	public RespiratorySystemBase RespiratorySystem => respiratorySystem;

	[CanBeNull]
	private MetabolismSystemV2 metabolism;
	public MetabolismSystemV2 Metabolism => metabolism;

	private bool isDead = false;
	public bool IsDead => isDead;

	public bool IsCrit => ConsciousState == ConsciousState.UNCONSCIOUS;
	public bool IsSoftCrit => ConsciousState == ConsciousState.BARELY_CONSCIOUS;

	public bool InCardiacArrest => circulatorySystem != null && circulatorySystem.HeartIsStopped;

	private List<ImplantBase> implantList = new List<ImplantBase>();

	public List<ImplantBase> ImplantList => implantList;

	public event Action<GameObject> applyDamageEvent;

	public event Action OnDeathNotifyEvent;

	//how on fire we are, sames as tg fire_stacks. 0 = not on fire.
	//It's called "stacks" but it's really just a floating point value that
	//can go up or down based on possible sources of being on fire. Max seems to be 20 in tg.
	[SyncVar(hook = nameof(SyncFireStacks))]
	private float fireStacks;
	private float maxFireStacks = 5f;
	private bool maxFireStacksReached = false;

	/// <summary>
	/// How on fire we are. Exists client side - synced with server.
	/// </summary>
	public float FireStacks => fireStacks;

	/// <summary>
	/// Client side event which fires when this object's fire status changes
	/// (becoming on fire, extinguishing, etc...). Use this to update
	/// burning sprites.
	/// </summary>
	[NonSerialized]
	public FireStackEvent OnClientFireStacksChange = new FireStackEvent();


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
		if (registerTile) return;
		registerTile = GetComponent<RegisterTile>();
		respiratorySystem = GetComponent<RespiratorySystemBase>();
		circulatorySystem = GetComponent<CirculatorySystemBase>();
		//Always include blood for living entities:
	}

	public override void OnStartServer()
	{
		EnsureInit();
		mobID = PlayerManager.Instance.GetMobID();

		//Generate BloodType and DNA
		DNABloodType = new DNAandBloodType();
	}

	/// <summary>
	/// Adds a new implant to the health master.
	/// This is NOT how implants should be added, it is called automatically by the body part container system!
	/// </summary>
	/// <param name="implant"></param>
	public void AddNewImplant(ImplantBase implant)
	{
		implantList.Add(implant);
	}

	public void RemoveImplant(ImplantBase implantBase)
	{
		implantList.Remove(implantBase);
	}


	public override void OnStartClient()
	{
		base.OnStartClient();
		EnsureInit();
		StartCoroutine(WaitForClientLoad());
	}

	private void UpdateMe()
	{
		foreach (ImplantBase implant in implantList)
		{
			implant.ImplantUpdate(this);
		}
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

		/// <summary>
	///  Apply Damage to the whole body of this Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount. will be distributed evenly across all bodyparts</param>
	/// <param name="attackType">type of attack that is causing the damage</param>
	/// <param name="damageType">The Type of Damage</param>
	[Server]
	public void ApplyDamage(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType)
	{
		//TODO: Reimplement
	}

	/// <summary>
	///  Apply Damage to random bodypart of the Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount</param>
	/// <param name="attackType">type of attack that is causing the damage</param>
	/// <param name="damageType">The Type of Damage</param>
	[Server]
	public void ApplyDamageToBodypart(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType)
	{
		ApplyDamageToBodypart(damagedBy, damage, attackType, damageType, BodyPartType.Chest.Randomize(0));
	}

	/// <summary>
	///  Apply Damage to the Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount</param>
	/// <param name="attackType">type of attack that is causing the damage</param>
	/// <param name="damageType">The Type of Damage</param>
	/// <param name="bodyPartAim">Body Part that is affected</param>
	[Server]
	public virtual void ApplyDamageToBodypart(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType, BodyPartType bodyPartAim)
	{
		//TODO: Reimplement
	}

	private void TryGibbing(float damage)
	{
		//TODO: Reimplement
	}

	/// <summary>
	///  Apply healing to a living thing. Server Only
	/// </summary>
	/// <param name="healingItem">the item used for healing (bruise pack etc). Null if there is none</param>
	/// <param name="healAmt">Amount of healing to add</param>
	/// <param name="damageType">The Type of Damage To Heal</param>
	/// <param name="bodyPartAim">Body Part to heal</param>
	[Server]
	public virtual void HealDamage(GameObject healingItem, int healAmt,
		DamageType damageTypeToHeal, BodyPartType bodyPartAim)
	{
		//TODO: Reimplement
	}

	[Server]
	public void Harvest()
	{

		//Reimplement

		Gib();
	}

	[Server]
	protected virtual void Gib()
	{

		//TODO: Reimplement

		//never destroy players!
		Despawn.ServerSingle(gameObject);
	}

	/// ---------------------------
	/// CRIT + DEATH METHODS
	/// ---------------------------
	///Death from other causes
	public virtual void Death()
	{
		//TODO: Reimplemenmt
	}

	public virtual void AddSickness(Sickness sickness)
	{
		//TODO: Reimplement
	}

	public void UpdateClientHealthStats(float overallHealth)
	{
		//TODO: Reimplement
	}

	/// <summary>
	/// Updates the conscious state from the server via NetMsg
	/// </summary>
	public void UpdateClientConsciousState(ConsciousState proposedState)
	{
		ConsciousState = proposedState;
	}

	/// <summary>
	/// Updates the respiratory health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientRespiratoryStats(bool value)
	{
		respiratorySystem.IsSuffocating = value;
	}

	public void UpdateClientTemperatureStats(float value)
	{
		respiratorySystem.temperature = value;
	}

	public void UpdateClientPressureStats(float value)
	{
		respiratorySystem.pressure = value;
	}

	/// <summary>
	/// Updates the blood health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientBloodStats(int heartRate, float bloodVolume, float oxygenDamage, float toxinLevel)
	{
		//TODO: Reimplement bloodSystem.UpdateClientBloodStats(heartRate, bloodVolume, oxygenDamage, toxinLevel);
	}

	/// <summary>
	/// Updates the brain health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientBrainStats(bool isHusk, int brainDamage)
	{
		//TODO: Reimplement
	}

	public void ChangeFireStacks(float deltaValue)
	{
		SyncFireStacks(fireStacks, fireStacks + deltaValue);
	}

	private void SyncFireStacks(float oldValue, float newValue)
	{
		EnsureInit();
		this.fireStacks = Math.Max(0, newValue);
		OnClientFireStacksChange.Invoke(this.fireStacks);
	}

	public void Extinguish()
	{
		SyncFireStacks(fireStacks, 0);
	}

	public virtual string GetExamineText()
	{
		return "Weeee";
	}


}
