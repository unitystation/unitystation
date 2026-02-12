using System;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

namespace Chemistry
{
	[CreateAssetMenu(fileName = "reagent", menuName = "ScriptableObjects/Chemistry/Reagent")]
	public class Reagent : SOTracker , IEquatable<Reagent>
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

		//Every single reaction this chemical is used in
		[NonSerialized] public Reaction[] RelatedReactions = Array.Empty<Reaction>();

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

		public override void ReassignID() //Assuming it's a prefab Variant
		{
#if UNITY_EDITOR
			if (string.IsNullOrEmpty(foreverID))
			{
				foreverID = displayName;
				return;
			}

			base.ReassignID();
#endif
		}

		public string Name
		{
			get => displayName ?? name;
			set => displayName = value;
		}

		public SOTracker ReturnSOTracker()
		{
			return this;
		}

		public override string ToString()
		{
			return Name;
		}

		public bool Equals(Reagent other)
		{
			if (other.indexInSingleton == indexInSingleton)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool operator ==(Reagent obj1, Reagent obj2)
		{
			if (obj1 is null || obj2 is null)
			{
				return obj1 is null && obj2 is null;
			}
			else
			{
				return obj1.Equals(obj2);
			}
		}

		public static bool operator !=(Reagent obj1, Reagent obj2)
		{
			return !(obj1 == obj2);
		}

		public override int GetHashCode()
		{
			if (indexInSingleton == -1)
			{
				return base.GetHashCode();
			}

			return indexInSingleton;
		}
	}
}