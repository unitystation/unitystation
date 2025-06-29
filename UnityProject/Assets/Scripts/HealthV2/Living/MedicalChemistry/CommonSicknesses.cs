using Chemistry;
using HealthV2.Sickness;
using ScriptableObjects;
using UnityEngine;

/// <summary>
/// Used for getting sicknesses outside monobehaviours
/// </summary>
[CreateAssetMenu(fileName = "CommonSicknesses", menuName = "Singleton/Chemistry/CommonSicknesses")]
public class CommonSicknesses : SingletonScriptableObject<CommonSicknesses>
{
   public Reagent SpaceFluReagent = null;
   public Reagent SpaceCancerReagent = null;
   public Reagent ParanoiaReagent = null;

   public SerializableDictionary<Reagent, SicknessReaction> DiseaseReactionDictionary = new SerializableDictionary<Reagent, SicknessReaction>();
}
