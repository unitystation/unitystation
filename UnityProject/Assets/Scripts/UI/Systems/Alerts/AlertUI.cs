using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
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

		private System.Action magBootsAction;
		private System.Action onClickBuckled;

		// invoked when the restrained alert is clicked
		public void OnClickAlertRestrained()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			onClickBuckled?.Invoke();
		}

		// called when the buckled button is clicked
		public void OnClickCuffed()
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdTryUncuff();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		/// <summary>
		/// Called when the switch pickup mode action button is pressed
		/// </summary>
		public void OnClickSwitchPickupMode()
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdSwitchPickupMode();
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		/// <summary>
		/// Called when mag boots mode action button is pressed
		/// </summary>
		public void OnClickMagBoots()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			magBootsAction?.Invoke();
		}

		private void OnEnable()
		{
			EventManager.AddHandler(Event.RoundEnded, OnRoundEnd);
			EventManager.AddHandler(Event.PlayerDied, OnPlayerDie);
			EventManager.AddHandler(Event.PlayerSpawned, OnPlayerSpawn);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.RoundEnded, OnRoundEnd);
			EventManager.RemoveHandler(Event.PlayerDied, OnPlayerDie);
			EventManager.RemoveHandler(Event.PlayerSpawned, OnPlayerSpawn);
		}

		// hides alerts to be visible when player dies
		private void OnPlayerDie()
		{
			shouldHideAllButtons = true;

			magBoots.SetActive(false);
			buckled.SetActive(false);
			cuffed.SetActive(false);
			pickupMode.SetActive(false);
		}

		// allows alerts to be visible when player spawns / respawns
		private void OnPlayerSpawn()
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
		public void ToggleAlertBuckled(bool show, System.Action onClick)
		{
			if (show)
			{
				onClickBuckled = onClick;
			}

			if (shouldHideAllButtons == false)
			{
				buckled.SetActive(show);
			}
		}

		/// <summary>
		/// Toggle Alert UI button for cuffed
		/// </summary>
		/// <param name="show"></param>
		public void ToggleAlertCuffed(bool show)
		{
			if (shouldHideAllButtons == false)
			{
				cuffed.SetActive(show);
			}
		}

		/// <summary>
		/// Toggle Alert UI button for pickup mode
		/// </summary>
		/// <param name="show"></param>
		public void ToggleAlertPickupMode(bool show)
		{
			if (shouldHideAllButtons == false)
			{
				pickupMode.SetActive(show);
			}
		}

		/// <summary>
		/// Toggle Alert UI button for mag boots
		/// </summary>
		/// <param name="show"></param>
		public void ToggleAlertMagBoots(bool show, System.Action magAction)
		{
			if (show)
			{
				magBootsAction = magAction;
			}

			if (shouldHideAllButtons == false)
			{
				magBoots.SetActive(show);
			}
		}
	}
}
