using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Used for events that occur when using hand apply, though could add more events from the reagent container.
/// As the reagent container causes interactions to fail.
/// </summary>
public class ReagentContainerObjectInteractionScript : MonoBehaviour
{
	public OnHandApplyEvent OnHandApply = new OnHandApplyEvent();
	public class OnHandApplyEvent : UnityEvent<HandApply> { }

	public void TriggerEvent(HandApply interaction)
	{
		OnHandApply?.Invoke(interaction);
	}
}