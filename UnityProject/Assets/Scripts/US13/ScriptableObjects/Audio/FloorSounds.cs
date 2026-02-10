using System.Collections.Generic;
using UnityEngine;
using US13.Core.Addressables.Types;

namespace US13.ScriptableObjects.Audio
{
	[CreateAssetMenu(fileName = "FloorSounds", menuName = "ScriptableObjects/FloorSounds")]
	public class FloorSounds : ScriptableObject
	{
		public List<AddressableAudioSource> Barefoot = new List<AddressableAudioSource>();
		public List<AddressableAudioSource> Claw = new List<AddressableAudioSource>();
		public List<AddressableAudioSource> Shoes = new List<AddressableAudioSource>();
	}
}