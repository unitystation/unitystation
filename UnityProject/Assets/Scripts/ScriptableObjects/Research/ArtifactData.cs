using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.Systems.Research
{
	[CreateAssetMenu(fileName = "ArtifactDataSO", menuName = "ScriptableObjects/Systems/Research/ArtifactDataSO")]
	public class ArtifactDataSO : ScriptableObject
	{
		public SerializableDictionary<string, GameObject> compositionData = new SerializableDictionary<string, GameObject>();
	}
}
