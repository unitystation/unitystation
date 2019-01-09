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
    public bool IsHusk => brain == null;
    /// <summary>
    /// How damaged is the brain
    /// </summary>
    /// <returns>Percentage between 0% and 100%. 
    /// -1 means there is no brain present</returns>
    public int BrainDamageAmt { get { if (brain == null) { return -1; } return Mathf.Clamp(brain.BrainDamage, 0, 101); } }

    void Awake()
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
        //Only preform brain monitoring/updating on the server
        if (CustomNetworkManager.Instance._isServer)
        {
            MonitorBrain();
        }
    }

    void MonitorBrain()
    {
        //Body is dead, no use checking the brain
        if (livingHealthBehaviour.IsDead || brain == null)
        {
            return;
        }

        //TODO Do brain damage calculations using the infections list
        // Later on add cell damage to the calculation
    }
}