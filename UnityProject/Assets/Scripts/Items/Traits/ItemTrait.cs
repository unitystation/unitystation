
using ScriptableObjects;
using UnityEngine;

/// <summary>
/// Base class for the trait system. Defines a particular
/// trait than an item can have (assigned in ItemAttributes)
/// </summary>
[CreateAssetMenu(fileName = "ItemTrait", menuName = "Traits/ItemTrait")]
public class ItemTrait : SOTracker
{
	// Is used in editor, so "unused" warning is ignored.
	#pragma warning disable CS0414
	[TextArea]
	[SerializeField] string traitDescription = "Describe me!"; // A short description of the trait and what it does
	#pragma warning restore CS0414
}
