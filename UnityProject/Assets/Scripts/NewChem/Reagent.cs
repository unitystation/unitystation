using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry
{
	[CreateAssetMenu(fileName = "reagent", menuName = "ScriptableObjects/Chemistry/Reagent")]
	public class Reagent : ScriptableObject
	{
		[SerializeField] string displayName;
		public string description;
		public Color color;
		public string tasteDescription;
		public ReagentState state;

		public string Name => displayName ?? name;
	}
}
