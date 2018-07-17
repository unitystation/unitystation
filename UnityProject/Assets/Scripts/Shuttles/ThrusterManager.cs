using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrusterManager : MonoBehaviour {
    //Manages the engines on shuttle

    //public MatrixMove mm;
    public ShipThruster[] engines;

	void Start ()
    {
        engines = GetComponentsInChildren<ShipThruster>();
	}

    public void UpdateEngines()
    {
        foreach (ShipThruster e in engines)
        {
            e.UpdateEngineState();
        }
    }

    public void RotateParticle(Orientation oldOrientation, Orientation newOrientation)
    {
        foreach(ShipThruster e in engines)
        {
            e.RotateFX(newOrientation);
        }
    }
	
}
