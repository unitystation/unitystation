using System.Collections.Generic;
using UnityEngine;
using US13.HealthV2.Living.MedicalChemistry;

namespace US13.ScriptableObjects
{
	[CreateAssetMenu(fileName = "NewMetabolismReactionSet", menuName = "ScriptableObjects/Chemistry/MetabolismReactionSet")]
	public class MetabolismReactionSets : ScriptableObject

	{
		public List<MetabolismReaction> ALLMetabolismReactions = new List<MetabolismReaction>();
	}
}
