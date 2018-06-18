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

    //Rotates FX as ship rotates
    public void RotateFX(/*bool clockwise*/)
    {
        //Debug.Log("RotateFX" + particleFX.transform.parent.name);
        //var mainFXrot = particleFX.main.startRotation;
        var mainFXrot = particleFX.main.startRotationZ;
        var mainFX = particleFX.main;
        mainFX.startRotation3D = true;
        //mainFX.startRotationZ.constant += 90f;
        mainFXrot.constant += 90f;
        Debug.Log(particleFX.main.startRotationZ.constant);
    }


    bool EngineStatus() // Returns if engines are "on" (if ship is moving)
    {
        if (shipMM != null)
        {
            //Debug.Log(shipMM.serverTargetState.IsMoving);
            return shipMM.serverTargetState.IsMoving;
        }

        return false;
    }
	
}
