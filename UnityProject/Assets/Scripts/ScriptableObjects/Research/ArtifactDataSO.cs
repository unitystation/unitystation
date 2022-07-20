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

		public List<ArtifactAreaEffectList> AreaEffects = new List<ArtifactAreaEffectList>();
		public List<ArtifactFeedEffectList> FeedEffects = new List<ArtifactFeedEffectList>();
	}

	[System.Serializable]
	public class ArtifactAreaEffectList
	{
		public List<AreaArtifactEffect> AreaArtifactEffectList;
	}

	[System.Serializable]
	public class ArtifactFeedEffectList
	{
		public List<FeedArtifactEffect> FeedArtifactEffectList;
	}
}
