using UnityEngine;


/// <summary>
/// Defines a particular state that an object can be in, for use with Stateful component.
/// </summary>
[CreateAssetMenu(fileName = "StatefulState", menuName = "Interaction/Stateful/StatefulState")]
public class StatefulState : ScriptableObject
{
	// Ignore "never used" warning because it *is* used in the inspector!
	#pragma warning disable CS0414
	[Tooltip("A short description of what the state represents. For documentation only, not used for any game logic.")]
	[TextArea]
	[SerializeField]
	private string stateDescription = "Describe me!";
	#pragma warning restore CS0414

}