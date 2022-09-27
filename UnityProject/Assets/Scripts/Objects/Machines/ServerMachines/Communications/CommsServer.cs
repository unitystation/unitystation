using System;
using Communications;
using InGameEvents;
using Managers;
using Systems.Electricity;
using Systems.Explosions;
using UnityEngine;

namespace Objects.Machines.ServerMachines.Communications
{
	public class CommsServer : SignalReceiver, IEmpAble
	{
		[SerializeField] private BlackboxMachine blackbox;
		[SerializeField] private APCPoweredDevice apcPoweredDevice;
		private Integrity integrity;
		private bool isMalf = false;

		public int EmpResistence = 250;

		private void Awake()
		{
			if (integrity == null)
			{
				integrity = GetComponent<Integrity>();
				integrity.OnDamaged += DirtyRepair;
			}
			if (apcPoweredDevice == null)
			{
				apcPoweredDevice = GetComponent<APCPoweredDevice>();
				if (apcPoweredDevice.RelatedAPC == null) apcPoweredDevice.ConnectToClosestApc();
			}
		}

		private void OnEnable()
		{
			GameManager.Instance.CommsServers.Add(this);
		}

		private void OnDisable()
		{
			GameManager.Instance.CommsServers.Remove(this);
		}

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			if(apcPoweredDevice.State == PowerState.Off) return;
			if(message is not RadioMessageData c) return;
			var finalMessage = c.ChatEvent;
			if (isMalf)
			{
				finalMessage.message = EventProcessorOverload.ProcessMessage(c.ChatEvent.message);
			}
			//Not the best way to do this currently but we can worry about it later during the chat rework
			ChatRelay.Instance.PropagateChatToClients(finalMessage);
			if(blackbox != null) blackbox.StoreChatEvents(finalMessage);
		}

		private void DirtyRepair()
		{
			isMalf = false;
			SparkUtil.TrySpark(gameObject);
		}

		public void OnEmp(int EmpStrength)
		{
			if (EmpStrength > EmpResistence) apcPoweredDevice.RemoveFromAPC();
			isMalf = true;
		}

		public struct RadioMessageData : ISignalMessage
		{
			public ChatEvent ChatEvent;
		}
	}
}