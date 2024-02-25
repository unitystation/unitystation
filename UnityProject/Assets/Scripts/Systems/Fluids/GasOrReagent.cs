using System.Collections;
using System.Collections.Generic;
using Chemistry;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using UnityEngine;

[System.Serializable]
public class GasOrReagent
{
	public Reagent Reagent;
	public GasSO Gas;

	public float Amount;

}
