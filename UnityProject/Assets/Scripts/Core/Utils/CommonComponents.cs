using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonComponents : MonoBehaviour
{

	public UniversalObjectPhysics UniversalObjectPhysics => SafeGetComponent<UniversalObjectPhysics>();

	public RegisterTile RegisterTile => SafeGetComponent<RegisterTile>();

	public PlayerScript PlayerScript => SafeGetComponent<PlayerScript>();

	public Dictionary<Type, Component> dictionary = new Dictionary<Type, Component>();

	public T SafeGetComponent<T>() where T : Component
	{
		if (dictionary.ContainsKey(typeof(T)) == false)
		{
			dictionary[typeof(T)] = this.GetComponent<T>();
		}

		return dictionary[typeof(T)] as T;

	}

}
