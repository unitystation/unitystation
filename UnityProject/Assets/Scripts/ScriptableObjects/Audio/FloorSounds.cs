using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;

[CreateAssetMenu(fileName = "FloorSounds", menuName = "ScriptableObjects/FloorSounds")]
public class FloorSounds : ScriptableObject
{
	public List<AddressableAudioSource> Barefoot = new List<AddressableAudioSource>();
	public List<AddressableAudioSource> Claw = new List<AddressableAudioSource>();
	public List<AddressableAudioSource> Shoes = new List<AddressableAudioSource>();
}
//Sound override
//Suit
//Heavy