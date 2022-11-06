using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IClientSynchronisedEffect : IClientPlayerLeaveBody, IClientPlayerTransferProcess
{
	public uint OnPlayerID { get; }

	public bool IsOnLocalPlayer => OnPlayerID == PlayerManager.LocalPlayerScript.netId;

	public void ClientOnPlayerLeaveBody()
	{
		ApplyDefaultOrCurrentValues(true);
	}

	public void ClientOnPlayerTransferProcess()
	{
		ApplyDefaultOrCurrentValues(false);
	}

	public void ApplyDefaultOrCurrentValues(bool Default);

	public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
	{
		if (NetId.Empty != PreviouslyOn && NetId.Invalid != PreviouslyOn)
		{
			ClientSynchronisedEffectsManager.Instance.ClientUnRegisterOnBody(CurrentlyOn, this);
		}

		if (NetId.Empty != CurrentlyOn && NetId.Invalid != CurrentlyOn)
		{
			ClientSynchronisedEffectsManager.Instance.ClientRegisterOnBody(CurrentlyOn, this);
		}

	}
}
