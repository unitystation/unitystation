using Logs;
using UnityEngine;
using US13.Core.Chat;
using US13.Health.Objects;
using US13.Objects.Engineering;
using US13.Objects.Machines.ServerMachines.Communications;
using US13.ScriptableObjects.Telecomms;
using US13.Systems.Communications;
using US13.Systems.InGameEvents.InGameEventScripts;

namespace US13.Objects.Wallmounts.PublicTerminals.Modules
{
	public class PublicTerminalRadioModule : PublicTerminalModule, IChatInfluencer
	{
		[SerializeField] private APCPoweredDevice poweredDevice;
		[SerializeField] private Integrity integrity;
		[SerializeField] private SignalDataSO radioSO;
		private const float MINIMUM_DAMAGE_BEFORE_OBFUSCATION = 8f;


		public bool WillInfluenceChat()
		{
			// Don't do anything if this module isn't enabled
			if (IsActive == false) return false;
			if (poweredDevice == null || integrity == null)
			{
				Loggy.Error("[PublicTerminals/Modules] - Missing components detected on a terminal.");
				return false;
			}
			// Don't send anything if this terminal has no power
			return poweredDevice.State != PowerState.Off;
		}

		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			CommsServer.RadioMessageData msg = new CommsServer.RadioMessageData();
			// If the integrity of this terminal is so low, start scrambling text.
			if (integrity.integrity > MINIMUM_DAMAGE_BEFORE_OBFUSCATION)
			{
				msg.ChatEvent = chatToManipulate;
				Terminal.TrySendSignal(radioSO, msg);
				return chatToManipulate;
			}
			var scrambledText = chatToManipulate;
			scrambledText.message = EventProcessorOverload.ProcessMessage(scrambledText.message);
			msg.ChatEvent = scrambledText;
			Terminal.TrySendSignal(radioSO, msg);
			return scrambledText;
		}
	}
}