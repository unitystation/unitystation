using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrusterManager : MonoBehaviour {
    //Manages the engines on shuttle

    public ShipThruster[] engines;

	public float particleSpeedMultiplier = 1.5f;

	void Start ()
    {
        engines = GetComponentsInChildren<ShipThruster>();

		UpdateEngines();
	}

    public void UpdateEngines() //Checks all the engines and sees if they need to be turned on or off
    {
        foreach (ShipThruster e in engines)
        {
            e.UpdateEngineState();
        }
    }

	public void RotateParticle(Orientation oldOrientation, Orientation newOrientation) //Rotates FX direction based on ship oreintation
	{
		foreach (ShipThruster e in engines)
		{
			e.RotateFX(newOrientation);
		}
	}

	public void UpdateSpeed(float oldSpeed, float newSpeed) //Changes speed of particles based on speed of ship
	{
		foreach (ShipThruster e in engines)
		{
			e.SpeedChange(oldSpeed, newSpeed, particleSpeedMultiplier);
		}
	}
	
}
