using System.Collections.Generic;
using UnityEngine;

namespace AddressableReferences
{
	/// <summary>
	/// List of addressable reference of given type
	/// </summary>
	/// <typeparam name="T">The type that all addressable reference in this list should be</typeparam>
	public class AddressableReferenceList<T> where T : UnityEngine.Object
	{
		public List<AddressableReference<T>> AddressableReferences = new List<AddressableReference<T>>();
	}

	/// <summary>
	/// List of AddressableReference of type AudioSource
	/// </summary>
	public class AudioSourceAddressableReferenceList: AddressableReferenceList<AudioSource>
	{
	}
}
