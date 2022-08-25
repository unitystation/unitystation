using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Core.Utils;

namespace Lobby
{
	/// <summary>
	/// Scripting for the server join panel found in the lobby UI.
	/// </summary>
	public class JoinPanel : MonoBehaviour
	{
		[SerializeField]
		private InputField addressControl = default;
		[SerializeField]
		private InputField portControl = default;
		[SerializeField]
		private Text errorControl = default;

		[SerializeField]
		private Button backButton = default;
		[SerializeField]
		private Button joinButton = default;
		[SerializeField]
		private Button historyButton = default;

		private const string DefaultServerAddress = "127.0.0.1";
		private const ushort DefaultServerPort = 7777;

		private void Awake()
		{
			addressControl.onValueChanged.AddListener((_) => ClearError());
			addressControl.onEndEdit.AddListener((_) => ValidateAddress());
			addressControl.onSubmit.AddListener((_) => TryJoin());

			portControl.onValueChanged.AddListener((_) => ClearError());
			portControl.onEndEdit.AddListener((_) => ValidatePort());
			portControl.onSubmit.AddListener((_) => TryJoin());

			backButton.onClick.AddListener(OnBackBtn);
			joinButton.onClick.AddListener(OnJoinBtn);
			historyButton.onClick.AddListener(OnHistoryBtn);
		}

		private void OnEnable()
		{
			LoadLastServerDetails();
			joinButton.Select();
		}

		private void LoadLastServerDetails()
		{
			var lastServer = LobbyManager.Instance.ServerJoinHistory.FirstOrDefault();

			addressControl.text = lastServer.Address ?? DefaultServerAddress;
			portControl.text = (lastServer.Port.Equals(default) ? DefaultServerPort : lastServer.Port).ToString();
		}

		private void TryJoin()
		{
			if (ValidateInputs())
			{
				LobbyManager.Instance.JoinServer(addressControl.text, ushort.Parse(portControl.text));
			}
		}

		private void OnBackBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.UI.ShowMainPanel();
		}

		private void OnJoinBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			TryJoin();
		}

		private void OnHistoryBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.UI.ShowServerHistoryPanel();
		}

		#region Validation

		private bool ValidateInputs()
		{
			if (ValidateAddress() == false) return false;

			if (ValidatePort() == false) return false;

			return true;
		}

		private bool ValidateAddress()
		{
			var errorStrings = new Dictionary<ValidationUtils.ValidationError, string>
			{
				{ ValidationUtils.ValidationError.NullOrWhitespace, "Server address is required." },
				{ ValidationUtils.ValidationError.Invalid, "Server address is invalid." },
			};

			if (ValidationUtils.TryValidateAddress(addressControl.text, out var failReason) == false)
			{
				SetError(errorStrings[failReason]);
				return false;
			}

			ClearError();
			return true;
		}

		private bool ValidatePort()
		{
			var errorStrings = new Dictionary<ValidationUtils.ValidationError, string>
			{
				{ ValidationUtils.ValidationError.NullOrWhitespace, "Server port is required." },
				{ ValidationUtils.ValidationError.Invalid, "Server port is invalid." },
			};

			if (ValidationUtils.TryValidatePort(portControl.text, out var failReason) == false)
			{
				SetError(errorStrings[failReason]);
				return false;
			}

			ClearError();
			return true;
		}

		private void SetError(string message) => errorControl.text = message;

		private void ClearError() => errorControl.text = string.Empty;

		#endregion
	}
}
