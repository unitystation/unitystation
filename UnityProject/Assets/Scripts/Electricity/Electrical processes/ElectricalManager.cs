using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalManager : MonoBehaviour
{
	private static ElectricalManager electricalManager;
	public static ElectricalManager Instance
	{
		get
		{
			if (electricalManager == null)
			{
				electricalManager = FindObjectOfType<ElectricalManager>();
			}
			return electricalManager;
		}
	}

	private bool roundStartedServer = false;

	void Update()
	{
		if (roundStartedServer && CustomNetworkManager.Instance._isServer)
		{
			ElectricalSynchronisation.DoUpdate();
		}
	}

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundStarted, OnRoundStart);
		EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnd);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundStarted, OnRoundStart);
		EventManager.RemoveHandler(EVENT.RoundEnded, OnRoundEnd);
	}

	void OnRoundStart()
	{
		roundStartedServer = true;
		Logger.Log("Round Started", Category.Electrical);
	}

	void OnRoundEnd()
	{
		roundStartedServer = false;
		Logger.Log("Round Ended", Category.Electrical);
	}
}