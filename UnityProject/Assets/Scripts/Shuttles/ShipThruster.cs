using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipThruster : MonoBehaviour {

    public MatrixMove shipMM; // ship matrix move
    public ParticleSystem particleFX;

    public bool flipped = false; //Was particle startrotation flipped already horizontally?

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

    //Rotates FX as ship rotates
    public void RotateFX()
    {
        var mainFXrot = particleFX.main;
        var mainFX = particleFX.main;

        if (!flipped)
        {
            mainFX.startRotation = 90 * Mathf.Deg2Rad;
            flipped = true;
        }
        else
        {
            mainFX.startRotation = 0;
            flipped = false;
        }
    }

    bool EngineStatus() // Returns if engines are "on" (if ship is moving)
    {
        if (shipMM != null)
        {
            return shipMM.serverTargetState.IsMoving;
        }

        return false;
    }
	
}
