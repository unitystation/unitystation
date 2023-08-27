using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items;
using UnityEngine;

public class CommonComponents : MonoBehaviour
{

	public UniversalObjectPhysics UniversalObjectPhysics => SafeGetComponent<UniversalObjectPhysics>();

	public RegisterTile RegisterTile => SafeGetComponent<RegisterTile>();

	public PlayerScript PlayerScript => SafeGetComponent<PlayerScript>();

	public Rotatable Rotatable => SafeGetComponent<Rotatable>();

	public LivingHealthMasterBase LivingHealth => SafeGetComponent<LivingHealthMasterBase>();

	public ItemAttributesV2 ItemAttributes => SafeGetComponent<ItemAttributesV2>();

	public Dictionary<Type, Component> dictionary = new Dictionary<Type, Component>();

	public bool TrySafeGetComponent<T>(out T component) where T : Component
	{
		if (dictionary.ContainsKey(typeof(T)) == false)
		{
			dictionary[typeof(T)] = this.GetComponent<T>();
		}

		component = dictionary[typeof(T)] as T;
		return component != null;
	}

	public T SafeGetComponent<T>() where T : Component
	{
		if (dictionary.ContainsKey(typeof(T)) == false)
		{
			dictionary[typeof(T)] = this.GetComponent<T>();
		}

		return dictionary[typeof(T)] as T;
	}

}
