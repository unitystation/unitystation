
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Use this instead of normal MonoBehaviour if you want to show a tooltip for this gameObject
/// Don't forget to override Tooltip property
/// </summary>
public class TooltipMonoBehaviour : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	/// <summary>
	/// Override this to show a tooltip on hover
	/// </summary>
	public virtual string Tooltip => baseTooltip;

	public string baseTooltip;

	/// <summary>
	/// Override this to show a tooltip on cursor exit (intended for special cases)
	/// </summary>
	public virtual string ExitTooltip => baseExitTooltip;


	public string baseExitTooltip;

	public void OnPointerEnter(PointerEventData eventData)
	{
		UIManager.SetToolTip = Tooltip;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		UIManager.SetToolTip = ExitTooltip;
	}
}
