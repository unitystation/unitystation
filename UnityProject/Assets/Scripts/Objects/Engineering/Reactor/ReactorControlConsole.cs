using System;
using System.Text;
using Communications;
using InGameEvents;
using Objects.Machines.ServerMachines.Communications;
using Shared.Systems.ObjectConnection;
using Systems.Communications;
using Systems.Electricity;
using UnityEngine;

namespace Objects.Engineering.Reactor
{
	[RequireComponent(typeof(APCPoweredDevice))]
	[RequireComponent(typeof(Integrity))]
	public class ReactorControlConsole : SignalEmitter, IMultitoolSlaveable, IChatInfluencer
	{
		[SceneObjectReference] public ReactorGraphiteChamber ReactorChambers = null;

		private APCPoweredDevice poweredDevice;
		private Integrity integrity;
		private UniversalObjectPhysics objectPhysics;

		private ChatEvent chatEvent = new ChatEvent();
		private const ChatChannel ChatChannels = ChatChannel.Common;

		[SerializeField] private float chatAnnouncementCheckTime = 8f;

		private void Awake()
		{
			poweredDevice = GetComponent<APCPoweredDevice>();
			integrity = GetComponent<Integrity>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		private void Start()
		{
			if (CustomNetworkManager.IsServer) UpdateManager.Add(UpdateMe, chatAnnouncementCheckTime);
			chatEvent.channels = ChatChannels;
			chatEvent.VoiceLevel = Loudness.LOUD;
			chatEvent.position = objectPhysics.OfficialPosition;
			chatEvent.originator = gameObject;
			chatEvent.speaker = "[Reactor Control Console]";
#if UNITY_EDITOR
			chatAnnouncementCheckTime = 2f;
#endif
		}

		public void SuchControllRodDepth(float requestedDepth)
		{
			requestedDepth = requestedDepth.Clamp(0, 1);

			if (ReactorChambers != null)
			{
				ReactorChambers.SetControlRodDepth(requestedDepth);
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (ReactorChambers is null) return;
			if (ReactorChambers.MeltedDown == false || ReactorChambers.PresentNeutrons < 200) return;

			StringBuilder state = new StringBuilder();
			state.Append("Warning, Reactor Meltdown/Catastrophe imminent.").AppendLine();
			if (ReactorChambers.CurrentPressure >= ReactorChambers.MaxPressure / (decimal)1.35f)
			{
				state.Append("Pressure: ").Append(
					Math.Round((ReactorChambers.CurrentPressure /
					            ReactorChambers.MaxPressure) * 100))
					.Append(".");
			}

			if (ReactorChambers.Temperature >= ReactorChambers.RodMeltingTemperatureK)
			{
				state.Append(" Temperature: ").Append(ReactorChambers.GetTemperature()).Append(".");
			}

			// magic number copied from GUI_ReactorController
			if (ReactorChambers.PresentNeutrons >= 200)
			{
				state.Append(" Neutron levels Unstable. ");
			}

			chatEvent.message = state.ToString();
			InfluenceChat(chatEvent);
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.ReactorChamber;
		IMultitoolMasterable IMultitoolSlaveable.Master => ReactorChambers;
		bool IMultitoolSlaveable.RequireLink => true;
		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}
		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			ReactorChambers = master is ReactorGraphiteChamber reactor ? reactor : null;
		}

		#endregion

		#region Radio

		public bool WillInfluenceChat()
		{
			if (emmitableSignalData.Count == 0) return false;
			return poweredDevice.State >= PowerState.On;
		}

		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			CommsServer.RadioMessageData msg = new CommsServer.RadioMessageData();

			// If the integrity of this terminal is so low, start scrambling text.
			if (integrity.integrity > minimumDamageBeforeObfuscation)
			{
				msg.ChatEvent = chatToManipulate;
				TrySendSignal(emmitableSignalData[0], msg);
				return chatToManipulate;
			}

			var scrambledText = chatToManipulate;
			scrambledText.message = EventProcessorOverload.ProcessMessage(scrambledText.message);
			msg.ChatEvent = scrambledText;
			TrySendSignal(emmitableSignalData[0], msg);
			return scrambledText;
		}

		protected override bool SendSignalLogic()
		{
			return WillInfluenceChat() && GameManager.Instance.CommsServers.Count != 0;
		}

		public override void SignalFailed() { }

		#endregion
	}
}
