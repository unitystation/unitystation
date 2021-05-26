using System;
using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
	public class SecurityCamera : MonoBehaviour
	{
		private static Dictionary<SecurityCameraChannels, List<SecurityCamera>> cameras = new Dictionary<SecurityCameraChannels, List<SecurityCamera>>();
		public static Dictionary<SecurityCameraChannels, List<SecurityCamera>> Cameras => cameras;

		[SerializeField]
		private SecurityCameraChannels securityCameraChannel = SecurityCameraChannels.Station;

		private void OnEnable()
		{
			if (cameras.ContainsKey(securityCameraChannel) == false)
			{
				cameras.Add(securityCameraChannel, new List<SecurityCamera>{this});
				return;
			}

			cameras[securityCameraChannel].Add(this);
		}

		private void OnDisable()
		{
			cameras[securityCameraChannel].Remove(this);
		}
	}

	public enum SecurityCameraChannels
	{
		Station,
		Syndicate
	}
}
