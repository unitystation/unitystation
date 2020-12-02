using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Messages.Server;
using ScriptableObjects;
using Systems.GhostRoles;

namespace UI.Windows
{
	/// <summary>
	/// An entry for <see cref="GhostRoleWindow"/>, displaying relevant information about the role.
	/// Clicking on the entry will send a request for the local player assignment
	/// to the role (see <see cref="Messages.Client.RequestGhostRoleMessage"/>).
	/// </summary>
	public class GhostRoleWindowEntry : MonoBehaviour
	{
		[SerializeField]
		private Text nameLabel = default;
		[SerializeField]
		private TextMeshProUGUI descLabel = default;
		[SerializeField]
		private Text countdownLabel = default;
		[SerializeField]
		private Text responseMessageLabel = default;
		[SerializeField]
		private Text playerCountLabel = default;
		[SerializeField]
		private SpriteHandler spriteHandler = default;
		[SerializeField]
		private Color warningColor = Color.red;
		[SerializeField]
		private Color successColor = Color.green;
		[SerializeField]
		private GameObject waitingOnResponseOverlay = default;

		public uint Key { get; private set; }
		public GhostRoleClient Role { get; private set; }

		private Coroutine clearResponseMessage;
		private Coroutine responseOverlayCoroutine;

		private void OnDisable()
		{
			Role.OnTimerExpired -= RemoveEntry;
		}

		public void SetValues(uint key, GhostRoleClient role)
		{
			Key = key;
			Role = role;

			nameLabel.text = Role.RoleData.Name;
			descLabel.text = Role.RoleData.Description;
			spriteHandler.SetSpriteSO(Role.RoleData.Sprite);
			playerCountLabel.text = GeneratePlayerCountLabelText();
			if (Role.PlayerCount / Role.MaxPlayers > 0.8f || Role.MaxPlayers - Role.PlayerCount == 1)
			{
				playerCountLabel.color = warningColor;
			}
			
			Role.OnTimerExpired += RemoveEntry;

			GhostRoleManager.Instance.StartCoroutine(Countdown(Role.TimeRemaining));
		}

		public void OnEntryClicked()
		{
			responseOverlayCoroutine = StartCoroutine(ActivateWaitingOnResponseOverlay());
			GhostRoleManager.Instance.LocalGhostRequestRole(Key);
		}

		public void SetResponseMessage(GhostRoleResponseCode code)
		{
			StopCoroutine(responseOverlayCoroutine);
			waitingOnResponseOverlay.SetActive(false);

			// Ignore displaying this particular response message; the success message is more useful.
			if (code == GhostRoleResponseCode.AlreadyWaiting) return;

			if (code == GhostRoleResponseCode.Success)
			{
				GhostRoleManager.Instance.TryStopCoroutine(ref clearResponseMessage);
				responseMessageLabel.color = successColor;
			}
			else
			{
				responseMessageLabel.color = warningColor;
				GhostRoleManager.Instance.RestartCoroutine(ClearResponseMessage(), ref clearResponseMessage);
			}

			responseMessageLabel.text = GhostRoleResponseMessage.GetMessageText(code);
		}

		private void RemoveEntry()
		{
			UIManager.GhostRoleWindow.RemoveEntry(Key);
		}

		private string GeneratePlayerCountLabelText()
		{
			switch (Role.RoleData.PlayerCountType)
			{
				case GhostRolePlayerCountType.ShowMaxCount:
					return Role.MaxPlayers.ToString();
				case GhostRolePlayerCountType.ShowCurrentAndMaxCounts:
					return $"{Role.PlayerCount} / {Role.MaxPlayers}";
				case GhostRolePlayerCountType.ShowMinCurrentAndMaxCounts:
					return $"{Role.MinPlayers} / {Role.PlayerCount} / {Role.MaxPlayers}";
				default:
					return default;
			}
		}

		private IEnumerator Countdown(float time)
		{
			while ((time -= Time.deltaTime) > 5)
			{
				countdownLabel.text = Mathf.CeilToInt(time).ToString();
				yield return WaitFor.EndOfFrame;
			}

			countdownLabel.color = warningColor;

			while ((time -= Time.deltaTime) > 0)
			{
				countdownLabel.text = Mathf.CeilToInt(time).ToString();
				yield return WaitFor.EndOfFrame;
			}

			countdownLabel.text = default;
		}

		private IEnumerator ClearResponseMessage()
		{
			yield return WaitFor.Seconds(3);
			responseMessageLabel.text = default;
		}

		private IEnumerator ActivateWaitingOnResponseOverlay()
		{
			waitingOnResponseOverlay.SetActive(true);

			yield return WaitFor.Seconds(5);

			waitingOnResponseOverlay.SetActive(false);
			// The request response should have already removed the overlay, but if it timed out...
			SetResponseMessage(GhostRoleResponseCode.Error);
		}
	}
}
