using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Defines how ores will be generated.
/// </summary>
[CreateAssetMenu(fileName = "OreGeneratorConfig", menuName = "ScriptableObjects/OreGeneratorConfig")]
public class OreGeneratorConfig : ScriptableObject
{
	[Tooltip("0 to 100. Defines what percentage of mineable tiles will spawn ores.")]
	[FormerlySerializedAs("Density")] [SerializeField]
  	private int density = 3; //out of 100
	/// <summary>
	/// 0 to 100. Defines what percentage of mineable tiles will spawn ores.
	/// </summary>
    public int Density => density;

	[Tooltip("Probabilities for spawning each type of ore.")]
	[FormerlySerializedAs("FullList")] [SerializeField]
	private List<OreProbability> oreProbabilities = new List<OreProbability>();

	/// <summary>
	/// Probabilities for spawning each type of ore.
	/// </summary>
	public IEnumerable<OreProbability> OreProbabilities => oreProbabilities;
}
