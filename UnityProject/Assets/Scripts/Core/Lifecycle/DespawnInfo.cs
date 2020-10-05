
using System;
using UnityEngine;

/// <summary>
/// Describes (but does not actually perform) an attempt to despawn things.
/// This is used to perform the described despawn (in Despawn) as well as pass the information
/// to lifecycle hook interface implementers.
/// </summary>
public class DespawnInfo
{
	/// <summary>
	/// GameObject to despawn.
	/// </summary>
	public readonly GameObject GameObject;
	
	private DespawnInfo(GameObject gameObject)
	{
		GameObject = gameObject;
	}

	/// <summary>
	/// Despawn the specified game object
	/// </summary>
	/// <param name="toDespawn"></param>
	/// <returns></returns>
	public static DespawnInfo Single(GameObject toDespawn)
	{
		return new DespawnInfo(toDespawn);
	}
}
