using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Manages all the alerts that can show up on the HUD (other than what is managed by PlayerHealthUI).
///
/// TODO: Currently just a simple system that only supports buckling/unbuckling. Needs to be refactored and expanded
/// to support arbitrary alerts, simultaneously. Do NOT hardcode every possible alert here.
/// </summary>
public class AlertUI : MonoBehaviour
{
	[FormerlySerializedAs("restrained")]
	public GameObject buckled;

	public GameObject cuffed;
	public GameObject pickupMode;

	private Action onClickBuckled;

	//invoked when the restrained alert is clicked
	public void OnClickAlertRestrained()
	{
		SoundManager.Play("Click01");
		onClickBuckled?.Invoke();
	}

	//called when the buckled button is clicked
	public void OnClickCuffed()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdTryUncuff();
		SoundManager.Play("Click01");
	}

	/// <summary>
	/// Called when the switch pickup mode action button is pressed
	/// </summary>
	public void OnClickSwitchPickupMode()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdSwitchPickupMode();
		SoundManager.Play("Click01");
	}

	private void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnd);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundEnded, OnRoundEnd);
	}

	public void OnRoundEnd()
	{
		onClickBuckled = null;
		buckled.SetActive(false);
		cuffed.SetActive(false);
		pickupMode.SetActive(false);
	}


	/// <summary>
	/// Show/hide restrained alert
	/// </summary>
	/// <param name="show">whether it should be shown</param>
	/// <param name="onClick">if show=true, callback to invoke when the alert is clicked</param>
	public void ToggleAlertBuckled(bool show, Action onClick)
	{
		buckled.SetActive(show);
		if (show)
		{
			this.onClickBuckled = onClick;
		}
	}

	/// <summary>
	/// Toggle Alert UI button for cuffed
	/// </summary>
	/// <param name="show"></param>
	public void ToggleAlertCuffed(bool show)
	{
		cuffed.SetActive(show);
	}

	/// <summary>
	/// Toggle Alert UI button for pickup mode
	/// </summary>
	/// <param name="show"></param>
	public void ToggleAlertPickupMode(bool show)
	{
		pickupMode.SetActive(show);
	}

}
