using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


[CreateAssetMenu(fileName = "OreGeneration", menuName = "ScriptableObjects/OreGeneration", order = 1)]
public class OreGeneratorScriptableObject : ScriptableObject
{
  	public int Density = 3; //out of 100

	public List<WeightNStrength> FullList = new List<WeightNStrength>();
}
