using System;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "SOLightMountStatesMachine", menuName = "ScriptableObjects/States/SOLightMountStatesMachine", order = 0)]
	public class SOLightMountStatesMachine : UnityEngine.ScriptableObject
	{
		public SerializableDictionary<LightMountState, SOLightMountState> LightMountStates;
	}

}