using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Chemistry;
using Chemistry.Components;

/// <summary>
/// This class enables an object to be ground up by an All-In-One-Grinder.
/// </summary>
public class Grindable : MonoBehaviour
{
	[SerializeField]
	[Tooltip("What reagent(s) this GameObject becomes when ground.")]
	private SerializableDictionary<Reagent, float> groundReagents;
	/// <summary>
	/// Get the processed product of this object.
	/// </summary>
	public SerializableDictionary<Reagent, float> GroundReagents
	{
		get
		{
			if (UseReagentContainer)
			{

				return ReagentContainer.CurrentReagentMix.reagents;
			}
			else
			{
				return groundReagents;
			}
		}
	}

	public bool UseReagentContainer = false;

	private ReagentContainer ReagentContainer;

	private void Awake()
	{
		ReagentContainer = this.GetComponentCustom<ReagentContainer>();
	}
}
