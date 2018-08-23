using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipThruster : MonoBehaviour {

    public MatrixMove shipMatrixMove; // ship matrix move
    public ParticleSystem particleFX;

    void Awake() {
        //Gets ship matrix move by getting root (top parent) of current gameobject
        shipMatrixMove = transform.root.gameObject.GetComponent<MatrixMove>();
        particleFX = GetComponentInChildren<ParticleSystem>();
    }

    public void UpdateEngineState()
    {
		var emissionFX = particleFX.emission;
        if (EngineStatus())
        {
            emissionFX.enabled = true;
			//SpeedChange(0, shipMatrixMove.State.Speed, thrusterManager.particleSpeedMultiplier); //Set particle speed on engine updates, used for setting speed at beginning of flight.
		}
        else
        {
            emissionFX.enabled = false;
        }
    }

    //Rotates FX as ship rotates
    public void RotateFX(Orientation newOrientation)
    {
        var mainFX = particleFX.main;

        mainFX.startRotation = newOrientation.Degree * Mathf.Deg2Rad;
    }

	public void SpeedChange(float oldSpeed, float newSpeed, float particleSpeedMultiplier)
	{
		var mainFX = particleFX.main;

		mainFX.startSpeed = newSpeed * particleSpeedMultiplier;
	}

	bool EngineStatus() // Returns if engines are "on" (if ship is moving)
    {
        if (shipMatrixMove != null)
        {
            return shipMatrixMove.State.IsMoving;
		}

        return false;
    }
	
}
