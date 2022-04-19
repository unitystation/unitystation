using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.Systems.Research
{
	[CreateAssetMenu(fileName = "ArtifactData", menuName = "ScriptableObjects/Systems/Research/ArtifactData")]
	public class ArtifactData : ScriptableObject
	{
		public SerializableDictionary<string, GameObject> compositionData = new SerializableDictionary<string, GameObject>();
	}
}
