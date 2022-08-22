using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Chemistry;

/// <summary>
/// This class enables an object to be ground up by an All-In-One-Grinder.
/// </summary>
public class Grindable : MonoBehaviour
{
	[SerializeField]
	[Tooltip("What reagent(s) this GameObject becomes when ground.")]
	private SerializableDictionary<Reagent, int> groundReagents;
	/// <summary>
	/// Get the processed product of this object.
	/// </summary>
	public SerializableDictionary<Reagent, int> GroundReagents => groundReagents;
}
