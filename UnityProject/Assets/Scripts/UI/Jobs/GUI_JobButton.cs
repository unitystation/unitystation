using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Script for the buttons on the job selection screen.
/// </summary>
public class GUI_JobButton : MonoBehaviour, IPointerEnterHandler
{
	public UnityEvent onPointerEnter;

	/// <summary>
	/// Invokes the listeners of onPointerEnter.
	/// </summary>
	public void OnPointerEnter(PointerEventData eventData)
	{
		onPointerEnter.Invoke();
		print("poop");
	}
}
