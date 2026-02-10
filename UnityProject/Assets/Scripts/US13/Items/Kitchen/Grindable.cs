using Chemistry;
using UnityEngine;
using US13.ChemistryComponents;
using Util;

namespace US13.Items.Kitchen
{
	/// <summary>
	/// This class enables an object to be ground up by an All-In-One-Grinder.
	/// </summary>
	public class Grindable : MonoBehaviour
	{
		public bool UseReagentContainer = false;

		[SerializeField]
		[Tooltip("What reagent(s) this GameObject becomes when ground.")]
		private SerializableDictionary<Reagent, float> groundReagents;
		/// <summary>
		/// Get the processed product of this object.
		/// </summary>
		public SerializableDictionary<Reagent, float> GroundReagents
		{
			get
			{
				if (UseReagentContainer)
				{

					return ReagentContainer.CurrentReagentMix.reagents;
				}
				else
				{
					return groundReagents;
				}
			}
		}



		private ReagentContainer ReagentContainer;

		private void Awake()
		{
			ReagentContainer = this.GetComponentCustom<ReagentContainer>();
		}
	}
}
