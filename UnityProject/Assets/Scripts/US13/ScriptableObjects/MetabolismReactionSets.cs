using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.HealthV2.Living.MedicalChemistry;

namespace US13.ScriptableObjects
{
	[CreateAssetMenu(fileName = "NewMetabolismReactionSet", menuName = "ScriptableObjects/Chemistry/MetabolismReactionSet")]
	public class MetabolismReactionSets : ScriptableObject

	{
		public List<MetabolismReaction> ALLMetabolismReactions = new List<MetabolismReaction>();


		[NaughtyAttributes.Button()]
		public void RemoveDuplicate()
		{
			var aa = ALLMetabolismReactions.ToHashSet();
			ALLMetabolismReactions = aa.ToList();
		}
	}
}
