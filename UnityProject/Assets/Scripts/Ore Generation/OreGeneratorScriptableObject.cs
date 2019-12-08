using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


[CreateAssetMenu(fileName = "OreGeneration", menuName = "ScriptableObjects/OreGeneration", order = 1)]
public class OreGeneratorScriptableObject : ScriptableObject
{
  	public int Density = 3; //out of 100

	public WeightNStrength IronBlockWeight;
	public WeightNStrength PlasmaBlockWeight;
	public WeightNStrength SilverBlockWeight;
	public WeightNStrength GoldBlockWeight;
	public WeightNStrength UraniumBlockWeight;
	public WeightNStrength BlueSpaceBlockWeight;
	public WeightNStrength TitaniumBlockWeight;
	public WeightNStrength DiamondBlockWeight;
	public WeightNStrength BananiumBlockWeight;


	public LayerTile Iron;
	public LayerTile Plasma;
	public LayerTile Silver;
	public LayerTile Gold;
	public LayerTile Uranium;
	public LayerTile BlueSpace;
	public LayerTile Titanium;
	public LayerTile Diamond;
	public LayerTile Bananium;
}
