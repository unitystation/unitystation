using System.Collections.Generic;
using UnityEngine;
using US13.ScriptableObjects;

namespace US13.Objects
{
	public class PassableExclusionHolder : MonoBehaviour
	{
		public List<PassableExclusionTrait> passableExclusions = new List<PassableExclusionTrait>();
	}
}
