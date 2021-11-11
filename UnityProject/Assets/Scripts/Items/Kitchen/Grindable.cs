using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Systems.Chemistry;

/// <summary>
/// This class enables an object to be ground up by an All-In-One-Grinder.
/// </summary>
public class Grindable : MonoBehaviour
{
	[SerializeField]
	[Tooltip("What reagent(s) this GameObject becomes when ground.")]
	private DictionaryReagentInt groundReagents;
	/// <summary>
	/// Get the processed product of this object.
	/// </summary>
	public DictionaryReagentInt GroundReagents => groundReagents;
}
