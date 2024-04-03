using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecureStuff;
using UnityEngine;

namespace SecureStuff
{
	public static class MicrophoneAccess
	{
		private static bool MicEnabled = false;

		public static void End(string deviceName)
		{
			Microphone.End(deviceName);
		}

		public static List<string> GetDevices()
		{
			return Microphone.devices.ToList();
		}

		public static bool IsRecording(string deviceName)
		{
			return Microphone.IsRecording(deviceName);
		}

		public static int GetPosition(string deviceName)
		{
			if (HubValidation.TrustedMode == false && MicEnabled == false)
			{
				//TODO TEMP
				//return 0;
			}
			return Microphone.GetPosition(deviceName);
		}

		public static async Task<AudioClip> Start(string deviceName, bool loop, int lengthSec, int frequency,
			string JustificationReason)
		{
			MicEnabled = await HubValidation.RequestMicrophoneAccess(JustificationReason);
			if (HubValidation.TrustedMode == false && MicEnabled == false)
			{
				//TODO TEMP
				//return null;
			}
			return Microphone.Start(deviceName, loop, lengthSec, frequency);
		}
	}
}