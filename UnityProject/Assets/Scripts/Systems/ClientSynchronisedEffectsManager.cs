using System.Collections;
using System.Collections.Generic;
using Shared.Managers;
using Systems.Radiation;
using UnityEngine;

public class ClientSynchronisedEffectsManager : SingletonManager<ClientSynchronisedEffectsManager>
{

	public Dictionary<uint, List<IClientSynchronisedEffect>> Data =
		new Dictionary<uint, List<IClientSynchronisedEffect>>();

	private void OnEnable()
	{
		EventManager.AddHandler(Event.RoundEnded, ClearData);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(Event.RoundEnded, ClearData);
	}


	private void ClearData()
	{
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
		if (Data.ContainsKey(BodyID))
		{
			foreach (var BodyValues in Data[BodyID])
			{
				BodyValues.ClientOnPlayerTransferProcess();
			}
		}
	}
}
