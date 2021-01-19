using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for the passable exclusion trait system. Defines a particular
/// trait than an object can have (assigned in Object Behaviour), needs to be on the object and the thing trying to
/// move to the object(assigned in the passable exclusions monobehaviour). Eg on spore to blob tile
/// </summary>
[CreateAssetMenu(fileName = "PassableExclusionTrait", menuName = "Traits/PassableExclusionTrait")]
public class PassableExclusionTrait : ScriptableObject
{
	// Is used in editor, so "unused" warning is ignored.
	#pragma warning disable CS0414
	[TextArea]
	[SerializeField] string traitDescription = "Describe me!"; // A short description of the trait and what it does
	#pragma warning restore CS0414
}
