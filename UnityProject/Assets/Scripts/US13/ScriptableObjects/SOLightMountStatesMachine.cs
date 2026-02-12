using UnityEngine;

namespace US13.ScriptableObjects
{
	[CreateAssetMenu(fileName = "SOLightMountStatesMachine", menuName = "ScriptableObjects/States/SOLightMountStatesMachine", order = 0)]
	public class SOLightMountStatesMachine : UnityEngine.ScriptableObject
	{
		public SerializableDictionary<LightMountState, SOLightMountState> LightMountStates;
	}

}