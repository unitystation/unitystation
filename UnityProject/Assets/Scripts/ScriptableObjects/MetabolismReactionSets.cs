using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewMetabolismReactionSet", menuName = "ScriptableObjects/Chemistry/MetabolismReactionSet")]
public class MetabolismReactionSets : ScriptableObject

{
	public List<MetabolismReaction> ALLMetabolismReactions = new List<MetabolismReaction>();
}
