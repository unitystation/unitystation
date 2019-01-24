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
    private float tick = 0f;
    //The amount of time the brain has been starved of oxygen
    private float noOxygenTime = 0f;
    private bool countOxygenLoss = false;
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
        UpdateManager.Instance.Add(UpdateMe);
    }

    void OnDisable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.Remove(UpdateMe);
    }

    // Controlled via UpdateManager
    void UpdateMe()
    {
        if (!init)
        {
            return;
        }
        //Server Only:
        if (CustomNetworkManager.Instance._isServer)
        {
            tick += Time.deltaTime;
            if (tick >= tickRate)
            {
                tick = 0f;
                MonitorBrain();
            }
            if (countOxygenLoss)
            {
                //Calculate how long oxygen has been starved for 
                CheckOxygenLossDamage();
            }
        }
    }

    void MonitorBrain()
    {
        //Body is dead, no use checking the brain
        if (livingHealthBehaviour.IsDead || brain == null)
        {
            return;
        }

        //No oxygen is getting to the brain
        if (respiratorySystem.IsSuffocating || bloodSystem.OxygenLevel < 5)
        {
            noOxygenTime = 0f;
            countOxygenLoss = true;
        }

        //TODO Do brain damage calculations using the infections list
        // Later on add cell damage to the calculation

        //TODO alcohol level in blood, make it affect speech and movement

        //TODO monitor elements in the blood stream. If oxygen is too low then begin an oxygen deprivation count
        //use this value for brain damage calculations
    }

    /// <summary>
    /// Determine Brain Damage from Oxygen Loss
    /// (25% brain damage for each minute of starvation after the 2 minute mark)
    /// </summary>
    void CheckOxygenLossDamage()
    {
        noOxygenTime += Time.deltaTime;

        //If player starts breathing again stop calculating oxygen loss:
        if (!respiratorySystem.IsSuffocating && bloodSystem.OxygenLevel >= 5)
        {
            countOxygenLoss = false;
            return;
        }

        if (noOxygenTime > 120f && noOxygenTime <= 180f && BrainDamageAmt < 25)
        {
            if (brain != null)brain.BrainDamage = 25;
        }
        if (noOxygenTime > 180f && noOxygenTime <= 240f && BrainDamageAmt < 50)
        {
            if (brain != null)brain.BrainDamage = 50;
        }
        if (noOxygenTime > 240f && noOxygenTime <= 300f && BrainDamageAmt < 75)
        {
            if (brain != null)brain.BrainDamage = 75;
        }
        if (noOxygenTime > 300f && noOxygenTime <= 360f && BrainDamageAmt < 100)
        {
            if (brain != null)
            {
                brain.BrainDamage = 100;
                //Player cannot survive full brain damage amounts
                if (!livingHealthBehaviour.IsDead)
                {
                    livingHealthBehaviour.Death();
                    countOxygenLoss = false;
                }
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