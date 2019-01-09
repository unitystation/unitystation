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

    void Awake()
    {
        InitSystem();
    }

    void InitSystem()
    {
        //Server only
        if (CustomNetworkManager.Instance._isServer)
        {
            //Spawn a brain and connect the brain to this living entity
            brain = new Brain();
            brain.ConnectBrainToBody(gameObject);
        }
    }
}