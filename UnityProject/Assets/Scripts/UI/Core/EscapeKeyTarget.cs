﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This component allows the game object to be disabled with the escape key automatically
/// It pushes the object to the escape key target stack when it's enabled, and pops it when it's disabled.
/// </summary>
public class EscapeKeyTarget : MonoBehaviour {
	[SerializeField]
	[Tooltip("What to invoke when this component receives the escape command, other than disabling if DisableOnEscape is true.")]
	private UnityEvent OnEscapeKey = new UnityEvent();

	[SerializeField]
	[Tooltip("If true, disables the game object when escape is recieved after calling OnEscapeKey")]
	private bool DisableOnEscape = true;

	/// <summary>
	/// A linked list which keeps track of all the EscapeKeyTargets so they can be closed later
	/// </summary>
	private static LinkedList<EscapeKeyTarget> Targets = new LinkedList<EscapeKeyTarget>();

	/// <summary>
	/// Handles escape key presses. Will close the most recently opened EscapeKeyTarget or will open the main menu if there are none.
	/// </summary>
	public static void HandleEscapeKey()
	{
		if(Targets.Count > 0)
		{
			EscapeKeyTarget escapeKeyTarget = Targets.Last.Value;
			escapeKeyTarget.OnEscapeKey.Invoke();
			if (escapeKeyTarget.DisableOnEscape)
			{
				// Close the escape key target at the top of the stack
				GUI_IngameMenu.Instance.CloseMenuPanel(escapeKeyTarget.gameObject);
			}
		}
		else if (GameData.IsInGame)
		{
			// Player is in-game and no escape key targets on the stack, so open the in-game menu
			GUI_IngameMenu.Instance.OpenMenuPanel();
		}
	}

	void OnEnable()
	{
		// Add this object to the top of the stack so Esc will close it next
		Logger.Log("Adding escape key target: " + this.name, Category.UserInput);
		Targets.AddLast(this);
	}
	void OnDisable()
	{
		// Remove the escape key target
		Logger.Log("Removing escape key target: " + this.name, Category.UserInput);
		Targets.Remove(this);
	}
}
