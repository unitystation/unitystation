using System;
using Cysharp.Threading.Tasks;
using Logs;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using US13.Systems.NtOS.Core;
using US13.UI.Core.Net;

namespace US13.UI.Core.NtOS
{
	public class TabNtOS : NetTab
	{
		private NtOS_Device currentDevice;
		[SerializeField, BoxGroup("Setup")] private EmptyItemList outputEntries;
		[SerializeField, BoxGroup("Setup")] private TMP_InputField inputField;

		private void Start()
		{
			inputField ??= GetComponentInChildren<TMP_InputField>();
			inputField.onSubmit.AddListener(OnEnterCommand);
			_ = WaitForProvider();
		}

		private async UniTask WaitForProvider()
		{
			var count = 0;
			while (Provider == null || count < 50)
			{
				await UniTask.WaitForEndOfFrame();
				count++;
			}
			currentDevice = Provider.GetComponent<NtOS_Device>();
			Loggy.Info($"NtOS device found: {currentDevice}");
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
			var newEntry = outputEntries.AddItem();
			var NtEntry = newEntry.GetComponent<NtOS_OutputEntry>();
			currentDevice.ExecuteCommand(command, NtEntry);
			inputField.text = "";
		}
	}
}