using SecureStuff;
using UnityEngine;
using US13.Managers.NetworkManagement;
using US13.Messages.Server.AdminTools;

namespace US13.Managers
{
	public class ProfileManagerWrapper : MonoBehaviour
	{


		public void Awake()
		{
			SafeProfileManager.ProfileBegin += ProfileBegin;
			SafeProfileManager.ProfileEnd += ProfileEnd;
		}


		public void OnDestroy()
		{
			SafeProfileManager.ProfileBegin -= ProfileBegin;
			SafeProfileManager.ProfileEnd -= ProfileEnd;
		}

		public void ProfileBegin()
		{
			UpdateManager.UpdateManager.Instance.Profile = true;
		}

		public void ProfileEnd()
		{
			UpdateManager.UpdateManager.Instance.Profile = false;

			if (CustomNetworkManager.IsServer)
			{
				ProfileMessage.SendToApplicable();
			}
		}
	}
}
