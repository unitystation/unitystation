using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IClientSynchronisedEffect : IClientPlayerLeaveBody, IClientPlayerTransferProcess
{
	public uint OnPlayerID { get; }

	public bool IsOnLocalPlayer => PlayerManager.LocalMindScript != null
	                               && PlayerManager.LocalMindScript.IsRelatedToObject(OnPlayerID.NetIdToGameObject())
	                               && PlayerManager.LocalMindScript.IsGhosting == false;

	void IClientPlayerLeaveBody.ClientOnPlayerLeaveBody()
	{
		ApplyDefaultOrCurrentValues(true);
	}

	void IClientPlayerTransferProcess.ClientOnPlayerTransferProcess()
	{
		ApplyDefaultOrCurrentValues(false);
	}

	/// <summary>
	/// Applies the correct synced values on a player based on a event. (I.e: player ghost leaving it's body)
	/// </summary>
	/// <param name="Default"></param>
	public void ApplyDefaultOrCurrentValues(bool Default);

	public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn);

	public void ImplementationSyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
	{
		if (NetId.Empty != PreviouslyOn && NetId.Invalid != PreviouslyOn)
		{
			ClientSynchronisedEffectsManager.Instance.ClientUnRegisterOnBody(PreviouslyOn, this);
			if (PlayerManager.LocalMindScript.OrNull()?.CurrentlyControllingObject != null && PlayerManager.LocalMindScript.CurrentlyControllingObject.NetId() == PreviouslyOn)
			{
				ApplyDefaultOrCurrentValues(true);
			}
		}

		if (NetId.Empty != CurrentlyOn && NetId.Invalid != CurrentlyOn)
		{
			ClientSynchronisedEffectsManager.Instance.ClientRegisterOnBody(CurrentlyOn, this);
			if (PlayerManager.LocalMindScript.OrNull()?.CurrentlyControllingObject != null && PlayerManager.LocalMindScript.CurrentlyControllingObject.NetId()  == CurrentlyOn)
			{
				ApplyDefaultOrCurrentValues(false);
			}
		}

	}
}
