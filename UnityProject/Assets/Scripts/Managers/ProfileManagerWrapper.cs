using System;
using System.Collections;
using System.Collections.Generic;
using Messages.Server.AdminTools;
using SecureStuff;
using UnityEngine;

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
		UpdateManager.Instance.Profile = true;
	}

	public void ProfileEnd()
	{
		UpdateManager.Instance.Profile = false;

		if (CustomNetworkManager.IsServer)
		{
			ProfileMessage.SendToApplicable();
		}
	}
}
