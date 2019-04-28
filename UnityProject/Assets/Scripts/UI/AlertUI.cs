using System;
using UnityEngine;

/// <summary>
/// Manages all the alerts that can show up on the HUD (other than what is managed by PlayerHealthUI).
///
/// TODO: Currently just a simple system that only supports buckling/unbuckling. Needs to be refactored and expanded
/// to support arbitrary alerts, simultaneously. Do NOT hardcode every possible alert here.
/// </summary>
public class AlertUI : MonoBehaviour
{
	public GameObject restrained;
	private Action onClick;

	//invoked when the restrained alert is clicked
	public void OnClickAlertRestrained()
	{
		onClick?.Invoke();
	}


	/// <summary>
	/// Show/hide restrained alert
	/// </summary>
	/// <param name="show">whether it should be shown</param>
	/// <param name="onClick">if show=true, callback to invoke when the alert is clicked</param>
	public void ToggleAlertRestrained(bool show, Action onClick)
	{
		restrained.SetActive(show);
		if (show)
		{
			this.onClick = onClick;
		}
	}
}
