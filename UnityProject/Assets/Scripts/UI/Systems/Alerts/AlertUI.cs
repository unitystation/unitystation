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
	public GameObject magBoots;

	bool shouldHideAllButtons = false;

	private Action magBootsAction;
	private Action onClickBuckled;

	//invoked when the restrained alert is clicked
	public void OnClickAlertRestrained()
	{
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
		onClickBuckled?.Invoke();
	}

	//called when the buckled button is clicked
	public void OnClickCuffed()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdTryUncuff();
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
	}

	/// <summary>
	/// Called when the switch pickup mode action button is pressed
	/// </summary>
	public void OnClickSwitchPickupMode()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdSwitchPickupMode();
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
	}

	/// <summary>
	/// Called when mag boots mode action button is pressed
	/// </summary>
	public void OnClickMagBoots()
	{
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
		magBootsAction?.Invoke();
	}

	private void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnd);
		EventManager.AddHandler(EVENT.PlayerDied, OnPlayerDie);
		EventManager.AddHandler(EVENT.PlayerSpawned, OnPlayerSpawn);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundEnded, OnRoundEnd);
		EventManager.RemoveHandler(EVENT.PlayerDied, OnPlayerDie);
		EventManager.RemoveHandler(EVENT.PlayerSpawned, OnPlayerSpawn);
	}

	/* hides alerts to be visible when player dies */
	void OnPlayerDie()
	{
		shouldHideAllButtons = true;

		magBoots.SetActive(false);
		buckled.SetActive(false);
		cuffed.SetActive(false);
		pickupMode.SetActive(false);
	}

	/* allows alerts to be visible when player spawns/respawns */
	void OnPlayerSpawn()
	{
		shouldHideAllButtons = false;

		buckled.SetActive(PlayerManager.LocalPlayerScript.playerMove.IsBuckled);

		cuffed.SetActive(PlayerManager.LocalPlayerScript.playerMove.IsCuffed);

		// TODO: check if player spawns with something where pickupMode should be shown
		pickupMode.SetActive(false);
	}

	public void OnRoundEnd()
	{
		magBootsAction = null;
		onClickBuckled = null;
		shouldHideAllButtons = false;

		magBoots.SetActive(false);
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
		if (show)
		{
			this.onClickBuckled = onClick;
		}

		if (!shouldHideAllButtons)
			buckled.SetActive(show);
	}

	/// <summary>
	/// Toggle Alert UI button for cuffed
	/// </summary>
	/// <param name="show"></param>
	public void ToggleAlertCuffed(bool show)
	{
		if (!shouldHideAllButtons)
			cuffed.SetActive(show);
	}

	/// <summary>
	/// Toggle Alert UI button for pickup mode
	/// </summary>
	/// <param name="show"></param>
	public void ToggleAlertPickupMode(bool show)
	{
		if (!shouldHideAllButtons)
			pickupMode.SetActive(show);
	}

	/// <summary>
	/// Toggle Alert UI button for mag boots
	/// </summary>
	/// <param name="show"></param>
	public void ToggleAlertMagBoots(bool show, Action magAction)
	{
		if (show)
		{
			this.magBootsAction = magAction;
		}

		if (!shouldHideAllButtons)
			magBoots.SetActive(show);
	}
}
