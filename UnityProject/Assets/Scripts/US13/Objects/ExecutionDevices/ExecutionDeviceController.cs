using Logs;
using UnityEngine;
using US13.Core.Admin.Logs;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Player;
using US13.UI.Core.RightClick;
using Util;

namespace US13.Objects.ExecutionDevices
{
	public class ExecutionDeviceController : MonoBehaviour
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
				Loggy.Error($"[ExecutionDeviceController/ReleaseVictim] - There's no device interface on {gameObject.name}!");
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

	}
}