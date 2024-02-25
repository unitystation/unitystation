using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "ThrusterFuelReactions", menuName = "Singleton/ThrusterFuelReactions")]
public class ThrusterFuelReactions : SingletonScriptableObject<ThrusterFuelReactions>
{
	[System.Serializable]
    public class ThrusterMixAndEffect
    {
	    //TODO Fraction multiple?
	    public List<GasOrReagent> ChemicalMakeUp = new List<GasOrReagent>();

	    public float ConsumptionMultiplierEffect = 0;
	    public float ThrustMultiplierEffect = 0;

    }



    public List<ThrusterMixAndEffect> ReactionMixes = new List<ThrusterMixAndEffect>();




}
