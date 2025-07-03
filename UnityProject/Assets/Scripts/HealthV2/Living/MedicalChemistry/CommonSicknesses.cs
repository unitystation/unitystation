using Chemistry;
using HealthV2.Sickness;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Used for getting sicknesses outside monobehaviours
/// </summary>
[CreateAssetMenu(fileName = "CommonSicknesses", menuName = "Singleton/Chemistry/CommonSicknesses")]
public class CommonSicknesses : SingletonScriptableObject<CommonSicknesses>
{
   public Reagent SpaceFluReagent = null;
   public Reagent SpaceCancerReagent = null;
   public Reagent ParanoiaReagent = null;

   public SerializableDictionary<string, SicknessReaction> diseaseReactionDictionary = default;
}
