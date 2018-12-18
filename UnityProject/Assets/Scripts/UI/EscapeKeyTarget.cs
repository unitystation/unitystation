using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeKeyTarget : MonoBehaviour {
	// This component allows the object to be disabled with the escape key automatically
	// It pushes the object to the escape key target stack when it's enabled, and pops it when it's disabled

	/// <summary>
	/// This is the stack which keeps track of all the game objects so they can be closed later
	/// </summary>
	[HideInInspector]
	public static Stack<GameObject> TargetStack = new Stack<GameObject>();
	
	void OnEnable()
	{
		// Add this game object to the top of the stack so Esc will close it next
		TargetStack.Push(gameObject);
		// Logger.Log("Pushing escape key target stack: " + TargetStack.Peek().name, Category.UI);
	}
	void OnDisable()
	{
		// Revert back to the previous escape key target
		// Logger.Log("Popping escape key target stack: " + TargetStack.Peek().name, Category.UI);
		TargetStack.Pop();	
	}
}
