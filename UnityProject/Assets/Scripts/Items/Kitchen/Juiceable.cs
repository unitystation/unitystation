using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Chemistry;

/// <summary>
/// This class enables an object to be juiced by an All-In-One-Grinder.
/// </summary>
public class Juiceable : MonoBehaviour
{
	[SerializeField]
	[Tooltip("What reagent(s) this GameObject becomes when juiced.")]
	private DictionaryReagentInt juicedReagents;
	/// <summary>
	/// Get the processed product of this object.
	/// </summary>
	public DictionaryReagentInt JuicedReagents => juicedReagents;
}
