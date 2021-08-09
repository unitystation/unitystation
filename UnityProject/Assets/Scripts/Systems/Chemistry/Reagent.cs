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

		[SerializeField, HideInInspector]
		private int indexInSingleton = -1;

		/// <summary>
		/// 	Index in the chemistry reagents' singleton. Used in a client-server communication
		/// 	(btw we can't serialize	reagent - color field isn't serializable and also a serialization
		/// 	is less effective than using singelton indexes).
		/// 	You can set a proper value in the ChemistryReagentsSO singleton inspector (just press the button below).
		/// </summary>
		public int IndexInSingleton
		{
			get => indexInSingleton;
#if UNITY_EDITOR
			set => indexInSingleton = value;
#endif
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