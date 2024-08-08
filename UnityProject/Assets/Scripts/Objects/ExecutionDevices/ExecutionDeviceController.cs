using System;
using System.Globalization;
using AdminTools;
using Core.Admin.Logs;
using Logs;
using UnityEngine;

namespace Objects.ExecutionDevices
{
	public class ExecutionDeviceController : MonoBehaviour, IRightClickable
	{
		private IExecutionDevice device;
		public GameObject Victim { get; set; }

		private void Awake()
		{
			device ??= GetComponent<IExecutionDevice>();
		}

		private void OnDestroy()
		{
			device = null;
			Victim = null;
		}

		public void Execute(GameObject executioner = null)
		{
			if (Victim == null)
			{
				if (executioner != null) Chat.AddExamineMsg(executioner, "There's nothing to execute!");
				return;
			}
			StartCoroutine(device.ExecuteTarget());
			LogExecution(executioner);
		}

		public void ReleaseVictim()
		{
			if (device == null)
			{
				Loggy.LogError($"[ExecutionDeviceController/ReleaseVictim] - There's no device interface on {gameObject.name}!");
				return;
			}
			device.OnLeaveDevice(Victim);
		}

		private void LogExecution(GameObject executioner)
		{
			if (Victim == null || executioner == null) return;
			AdminLogsManager.AddNewLog(executioner,
				$"{executioner.Player().Mind.CurrentPlayScript.playerName} " +
				$"attempted to execute {Victim.ExpensiveName()} at {Victim.AssumedWorldPosServer()}.", LogCategory.MobDamage, Severity.DEATH);
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var newList = new RightClickableResult();
			newList.AddElement("Execute", () => Execute(PlayerManager.LocalPlayerObject));
			newList.AddElement("Release Victim", ReleaseVictim);
			return newList;
		}
	}
}