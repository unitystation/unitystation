using Chemistry;
using UnityEngine;

namespace US13.Items.Kitchen
{
	/// <summary>
	/// This class enables an object to be juiced by an All-In-One-Grinder.
	/// </summary>
	public class Juiceable : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("What reagent(s) this GameObject becomes when juiced.")]
		private SerializableDictionary<Reagent, int> juicedReagents;
		/// <summary>
		/// Get the processed product of this object.
		/// </summary>
		public SerializableDictionary<Reagent, int> JuicedReagents => juicedReagents;
	}
}
