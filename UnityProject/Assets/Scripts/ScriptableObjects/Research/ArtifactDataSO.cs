using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Research;

namespace ScriptableObjects.Systems.Research
{
	[CreateAssetMenu(fileName = "ArtifactDataSO", menuName = "ScriptableObjects/Systems/Research/ArtifactDataSO")]
	public class ArtifactDataSO : ScriptableObject
	{
		public SerializableDictionary<string, GameObject> compositionData = new SerializableDictionary<string, GameObject>();

		public ArtifactSprite[] OrganicSprites;
		public ItemTrait OrganicSampleTrait; //The required item trait to take samples from this artifact

		public ArtifactSprite[] GeologicalSprites;
		public ItemTrait GeologicalSampleTrait; //The required item trait to take samples from this artifact

		public ArtifactSprite[] MechanicalSprites;
		public ItemTrait MechanicalSampleTrait; //The required item trait to take samples from this artifact


		public List<ArtifactAreaEffectList> AreaEffects = new List<ArtifactAreaEffectList>();
		public List<ArtifactInteractEffectList> InteractEffects = new List<ArtifactInteractEffectList>();
		public List<ArtifactDamageEffectList> DamageEffect = new List<ArtifactDamageEffectList>();
	}

	[System.Serializable]
	public class ArtifactAreaEffectList
	{
		public List<AreaArtifactEffect> AreaArtifactEffectList;
	}

	[System.Serializable]
	public class ArtifactInteractEffectList
	{
		public List<InteractArtifactEffect> InteractArtifactEffectList;
	}

	[System.Serializable]
	public class ArtifactDamageEffectList
	{
		public List<ArtifactEffect> DamageArtifactEffectList;
	}
}
