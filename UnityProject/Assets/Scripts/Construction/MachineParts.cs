using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Machines
{
	[CreateAssetMenu(fileName = "MachineParts", menuName = "ScriptableObjects/MachineParts", order = 1)]
	public class SpawnManagerScriptableObject : ScriptableObject
	{
		public List<ItemTrait> ItemsTraits = new List<ItemTrait>();
	}
}