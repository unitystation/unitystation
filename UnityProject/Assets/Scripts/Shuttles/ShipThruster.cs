using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipThruster : MonoBehaviour {

    public MatrixMove shipMM; // ship matrix move
    public ParticleSystem particleFX;

    void Start() {
        //Gets ship matrix move by getting root (top parent) of current gameobject
        shipMM = transform.root.gameObject.GetComponent<MatrixMove>();

        particleFX = GetComponentInChildren<ParticleSystem>();
        UpdateEngineState();
    }

    public void UpdateEngineState()
    {
        var emissionFX = particleFX.emission;
        if (EngineStatus())
        {
            emissionFX.enabled = true;
        }
        else
        {
            emissionFX.enabled = false;
        }
    }

    //void StateChange()
    //{
    //    UpdateEngineState();
    //}

    bool EngineStatus()
    {
        if (shipMM != null)
        {
            Debug.Log(shipMM.serverTargetState.IsMoving);
            return shipMM.serverTargetState.IsMoving;
        }

        return false;
    }
	
}
