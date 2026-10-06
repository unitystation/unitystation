using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Logs;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Player;
using US13.Systems.NtOS.Core;
using US13.UI.Core.Net;

namespace US13.UI.Core.NtOS
{
	public class TabNtOS : NetTab
	{
		private NtOS_Device currentDevice;
		[SerializeField, BoxGroup("Setup")] private NtOS_OutputEntry output;
		[SerializeField, BoxGroup("Setup")] private TMP_InputField inputField;

		private List<string> inputHistory = new();
		private int historyIndex = 0;

		private void Start()
		{
			inputField ??= GetComponentInChildren<TMP_InputField>();
			inputField.onSubmit.AddListener(OnEnterCommand);
			_ = WaitForProvider();
			if (CustomNetworkManager.IsServer)
			{
				OnTabOpened.AddListener(TabOpened);
				OnTabClosed.AddListener(TabClosed);
			}
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.UpArrow) && inputHistory.Count > 0)
			{
				historyIndex++;
				if (historyIndex > inputHistory.Count - 1)
				{
					historyIndex = Mathf.Clamp(historyIndex, 0, inputHistory.Count - 1);
				}
				inputField.text = inputHistory[historyIndex];
				return;
			}
			if (Input.GetKeyDown(KeyCode.DownArrow) && inputHistory.Count > 0)
			{
				historyIndex--;
				if (historyIndex < 0)
				{
					historyIndex = Mathf.Clamp(historyIndex, 0, inputHistory.Count - 1);
				}
				inputField.text = inputHistory[historyIndex];
				return;
			}
			if (Input.anyKey)
			{
				historyIndex = 0;
			}
		}

		private async UniTask WaitForProvider()
		{
			var count = 0;
			while (Provider == null || count < 124)
			{
				await UniTask.WaitForEndOfFrame();
				count++;
			}
			currentDevice = Provider.GetComponent<NtOS_Device>();
			Loggy.Info($"NtOS device found: {currentDevice}");
			if (IsUnobserved == false)
			{
				// Call manually; OnTabOpened is invoked before the Provider is set,
				// so the initial invoke was missed.
				TabOpened();
			}
		}

		private void TabOpened(PlayerInfo newPeeper = default)
		{
			UpdateManager.Add(UpdateMe, 0.15f);
		}

		private void TabClosed(PlayerInfo oldPeeper = default)
		{
			// Remove listeners when unobserved (old peeper has not yet been removed).
			if (Peepers.Count <= 1)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			}
		}

		private void UpdateMe()
		{
			if (currentDevice == null) return;
			output.SetDisplayText(GetTextFromDeviceHistory());
		}

		private string GetTextFromDeviceHistory()
		{
			var combinedOutput = new StringBuilder();
			foreach (var h in currentDevice.History)
			{
				combinedOutput.AppendLine(h.Text.ToString());
			}
			return combinedOutput.ToString();
		}

		public override void RefreshTab()
		{
			base.RefreshTab();
			_ = WaitForProvider();
		}

		private void OnEnterCommand(string command)
		{
			if (string.IsNullOrEmpty(command)) return;
			if (currentDevice == null)
			{
				Loggy.Warning("NtOS device not found or still not ready.");
				return;
			}

			var playerInfo = PlayerManager.LocalPlayerScript.PlayerInfo;
			currentDevice.ExecuteCommand(command, playerInfo);
			inputHistory.Add(inputField.text);
			inputField.text = "";
		}
	}
}