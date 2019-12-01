using UnityEngine;


/// <summary>
/// Defines a particular state that an object can be in, for use with Stateful component.
/// </summary>
public class StatefulState : ScriptableObject
{
	[Tooltip("A short description of what the state represents. For documentation only, not used for any game logic.")]
	[TextArea]
	[SerializeField]
	private string stateDescription = "Describe me!";

}