using Chemistry;
using UnityEngine;
using US13.ScriptableObjects;

namespace US13.HealthV2.Living.MedicalChemistry
{
	/// <summary>
	/// Used for getting sicknesses outside monobehaviours
	/// </summary>
	[CreateAssetMenu(fileName = "CommonSicknesses", menuName = "Singleton/Chemistry/CommonSicknesses")]
	public class CommonSicknesses : SingletonScriptableObject<CommonSicknesses>
	{
		public Reagent SpaceFluReagent = null;
		public Reagent SpaceCancerReagent = null;
		public Reagent ParanoiaReagent = null;
		public Reagent SpaceColdReagent = null;


		public SerializableDictionary<string, SicknessReaction> diseaseReactionDictionary = default;
	}
}
