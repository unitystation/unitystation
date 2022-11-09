using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IClientSynchronisedEffect : IClientPlayerLeaveBody, IClientPlayerTransferProcess
{
	public uint OnPlayerID { get; }

	public bool IsOnLocalPlayer => OnPlayerID == PlayerManager.LocalPlayerScript.netId;

	void IClientPlayerLeaveBody.ClientOnPlayerLeaveBody()
	{
		ApplyDefaultOrCurrentValues(true);
	}

	void IClientPlayerTransferProcess.ClientOnPlayerTransferProcess()
	{
		ApplyDefaultOrCurrentValues(false);
	}

	public void ApplyDefaultOrCurrentValues(bool Default);

	public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn);

	public void ImplementationSyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
	{
		if (NetId.Empty != PreviouslyOn && NetId.Invalid != PreviouslyOn)
		{
			ClientSynchronisedEffectsManager.Instance.ClientUnRegisterOnBody(CurrentlyOn, this);
			if (PlayerManager.LocalPlayerScript.netId == PreviouslyOn)
			{
				ApplyDefaultOrCurrentValues(true);
			}
		}

		if (NetId.Empty != CurrentlyOn && NetId.Invalid != CurrentlyOn)
		{
			ClientSynchronisedEffectsManager.Instance.ClientRegisterOnBody(CurrentlyOn, this);
			if (PlayerManager.LocalPlayerScript.netId == CurrentlyOn)
			{
				ApplyDefaultOrCurrentValues(false);
			}
		}

	}
}
