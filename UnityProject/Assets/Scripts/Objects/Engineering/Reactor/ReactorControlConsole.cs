using System;
using System.Text;
using Communications;
using InGameEvents;
using Objects.Machines.ServerMachines.Communications;
using System.Collections.Generic;
using Core;
using Logs;
using SecureStuff;
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
		private const ChatChannel ChatChannels = ChatChannel.Engineering;

		[SerializeField] private float chatAnnouncementCheckTime = 8f;
		[field: SerializeField] public bool CanRelink { get; set; } = true;
		private void Awake()
		{
			poweredDevice = GetComponent<APCPoweredDevice>();
			integrity = GetComponent<Integrity>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			AddToReactorChambers();
		}

		private void Start()
		{
			if (CustomNetworkManager.IsServer) UpdateManager.Add(UpdateMe, chatAnnouncementCheckTime);
			chatEvent.channels = ChatChannels;
			chatEvent.VoiceLevel = Loudness.LOUD;
			chatEvent.position = objectPhysics.OfficialPosition;
			chatEvent.originator = gameObject;
			chatEvent.speaker = "[Reactor Control Console]";
		}
		public void SuchControlRodDepth(float requestedDepth)
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

		private float SecondSinceLastUpdate = -1;
		private float UpdatePeriodic = 300;


		private void UpdateMe()
		{
			if (ReactorChambers is null) return;
			if (ReactorChambers.MeltedDown == false || ReactorChambers.PresentNeutrons < 200) return;

			if (SecondSinceLastUpdate != -1)
			{
				SecondSinceLastUpdate += Time.deltaTime;
			}

			if (SecondSinceLastUpdate == -1 || SecondSinceLastUpdate > UpdatePeriodic)
			{
				SecondSinceLastUpdate = 0;
				StringBuilder state = new StringBuilder();
				chatEvent.channels = ChatChannels;
				state.AppendFormat("Warning, Reactor Meltdown/Catastrophe imminent.");
				if (ReactorChambers.CurrentPressure >= ReactorGraphiteChamber.MAX_CORE_PRESSURE / (decimal)1.35f)
				{
					state.AppendFormat("Pressure: ").AppendFormat(
							(Math.Round((ReactorChambers.CurrentPressure /
								ReactorGraphiteChamber.MAX_CORE_PRESSURE) * 100)).ToString())
						.Append(".");
				}

				if (ReactorChambers.Temperature >= ReactorChambers.RodMeltingTemperatureK)
				{
					state.AppendFormat(" Temperature: ").AppendFormat(ReactorChambers.GetTemperature().ToString()).AppendFormat(".");
				}

				// magic number copied from GUI_ReactorController
				if (ReactorChambers.PresentNeutrons >= 200)
				{
					state.AppendFormat(" Neutron levels Unstable. ");
				}

				chatEvent.message = state.ToString();
				InfluenceChat(chatEvent);

			}


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
			ReactorChambers = master is ReactorGraphiteChamber reactor ? reactor : null;
		}

		private void SetMaster(IMultitoolMasterable master)
		{

			RemoveToReactorChambers();
			ReactorChambers = master is ReactorGraphiteChamber reactor ? reactor : null;
			AddToReactorChambers();
		}

		private void AddToReactorChambers()
		{
			if (ReactorChambers != null)
			{
				if (ReactorChambers.ConnectedConsoles.Contains(this) == false)
				{
					ReactorChambers.ConnectedConsoles.Add(this);
				}
			}
		}

		private void RemoveToReactorChambers()
		{
			if (ReactorChambers != null)
			{
				if (ReactorChambers.ConnectedConsoles.Contains(this))
				{
					ReactorChambers.ConnectedConsoles.Remove(this);
				}
			}
		}

		public void OnDestroy()
		{
			RemoveToReactorChambers();
		}

		#endregion

		#region Radio

		public bool WillInfluenceChat()
		{
			if (emmitableSignalData.Count == 0) return false;
			return poweredDevice.State >= PowerState.On;
		}

		private DateTime ScrumLastAnnounced = new DateTime(0);

		public void AnnounceSCRAM()
		{
			try
			{
				if ((DateTime.Now - ScrumLastAnnounced).TotalSeconds > 60)
				{
					if (ReactorChambers.ControlRodDepthPercentage != 1 && ReactorChambers.MeltedDown == false)
					{
						ScrumLastAnnounced = DateTime.Now;
						chatEvent.message = "The reactor has triggered an automatic SCRAM!!";
						chatEvent.channels = ChatChannel.Common;
						InfluenceChat(chatEvent);
						chatEvent.channels = ChatChannel.Engineering;
						_ = SoundManager.PlayNetworked(CommonSounds.Instance.AnnouncementAlert);
					}
				}

			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}

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
