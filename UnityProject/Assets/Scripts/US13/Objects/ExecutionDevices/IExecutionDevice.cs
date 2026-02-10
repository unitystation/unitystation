using System.Collections;
using UnityEngine;

namespace US13.Objects.ExecutionDevices
{
	public interface IExecutionDevice
	{
		public ExecutionDeviceController Controller { get; set; }
		public void OnEnterDevice(GameObject target, GameObject executioner = null);
		public void OnLeaveDevice(GameObject target, GameObject executioner = null);
		public IEnumerator ExecuteTarget();
	}
}