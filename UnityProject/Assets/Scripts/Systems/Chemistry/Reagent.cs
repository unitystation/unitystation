using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry
{
	[CreateAssetMenu(fileName = "reagent", menuName = "ScriptableObjects/Chemistry/Reagent")]
	public class Reagent : ScriptableObject
	{
		[SerializeField]
		[Tooltip("This is optional")]
		string displayName;
		[TextArea]
		public string description;
		public Color color;
		public ReagentState state;
		public float heatDensity = 5f; // Number of joules of energy to raise one unit of Reagent By 1Degrees

		private int indexInSingleton = -1;

		/// <summary>
		/// 	Index in the chemistry reagents' singleton. Used in a client-server communication
		/// 	(btw we can't serialize	reagent - color field isn't serializable and also a serialization
		/// 	is less effective than using singelton indexes).
		/// </summary>
		public int IndexInSingleton
		{
			get => indexInSingleton;
			set => indexInSingleton = value;
		}

		public string Name
		{
			get => displayName ?? name;
			set => displayName = value;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}