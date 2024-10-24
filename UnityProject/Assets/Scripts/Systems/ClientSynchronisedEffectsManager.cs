using System;
using System.Collections;
using System.Collections.Generic;
using Shared.Managers;
using Systems.Radiation;
using UnityEngine;

public class ClientSynchronisedEffectsManager : SingletonManager<ClientSynchronisedEffectsManager>
{

	public Dictionary<uint, List<IClientSynchronisedEffect>> Data =
		new Dictionary<uint, List<IClientSynchronisedEffect>>();

	public static uint CurrentlyOn = 0;

	private void OnEnable()
	{
		EventManager.AddHandler(Event.RoundEnded, ClearData);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(Event.RoundEnded, ClearData);
	}


	public void ClearData()
	{
		CurrentlyOn = 0;
		Data.Clear();
	}

	public void ClientRegisterOnBody(uint BodyID, IClientSynchronisedEffect Effect)
	{
		if (Data.ContainsKey(BodyID) == false)
		{
			Data[BodyID] = new List<IClientSynchronisedEffect>();
		}

		Data[BodyID].Add(Effect);
	}

	public void ClientUnRegisterOnBody(uint BodyID, IClientSynchronisedEffect Effect)
	{
		if (Data.ContainsKey(BodyID) == false)
		{
			Data[BodyID] = new List<IClientSynchronisedEffect>();
		}

		if (Data[BodyID].Contains(Effect))
		{
			Data[BodyID].Remove(Effect);
		}
	}

	public void LeavingBody(uint BodyID)
	{
		if (CurrentlyOn == BodyID)
		{
			CurrentlyOn = 0;
		}

		if (Data.ContainsKey(BodyID))
		{
			foreach (var BodyValues in Data[BodyID])
			{
				BodyValues.ClientOnPlayerLeaveBody();
			}
		}
	}

	public void EnterBody(uint BodyID)
	{
		CurrentlyOn = BodyID;
		if (Data.ContainsKey(BodyID))
		{
			foreach (var BodyValues in Data[BodyID])
			{
				BodyValues.ClientOnPlayerTransferProcess();
			}
		}
	}
}
