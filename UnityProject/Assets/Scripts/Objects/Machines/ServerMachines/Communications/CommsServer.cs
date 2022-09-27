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
		private bool isMalfunctioning = false;

		public int EmpResistance = 250;

		private const int FIFTYPERCENT = 50;

		private void Awake()
		{
			if (integrity == null)
			{
				integrity = GetComponent<Integrity>();
				integrity.OnApplyDamage.AddListener(OnDamageReceived);
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
			integrity.OnApplyDamage.RemoveListener(OnDamageReceived);
		}

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			if(apcPoweredDevice.State == PowerState.Off) return;
			if(message is not RadioMessageData c) return;
			var finalMessage = c.ChatEvent;
			if (isMalfunctioning)
			{
				finalMessage.message = EventProcessorOverload.ProcessMessage(c.ChatEvent.message);
			}
			//Not the best way to do this currently but we can worry about it later during the chat rework
			ChatRelay.Instance.PropagateChatToClients(finalMessage);
			if(blackbox != null) blackbox.StoreChatEvents(finalMessage);
		}

		private void OnDamageReceived(DamageInfo damageInfo)
		{
			if (damageInfo.AttackType == AttackType.Melee)
			{
				isMalfunctioning = false;
			}
			SparkUtil.TrySpark(gameObject);
		}

		public void OnEmp(int EmpStrength)
		{
			if (EmpStrength > EmpResistance) apcPoweredDevice.RemoveFromAPC();
			isMalfunctioning = true;
		}

		public struct RadioMessageData : ISignalMessage
		{
			public ChatEvent ChatEvent;
		}
	}
}