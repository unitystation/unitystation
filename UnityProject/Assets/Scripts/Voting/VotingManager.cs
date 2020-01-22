using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Controls everything to do with player voting
/// </summary>
public class VotingManager : NetworkBehaviour
{
	public static VotingManager Instance;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	[Server]
	public void TryInitiateRestartVote()
	{

	}

	[Server]
	public void RegisterRestartVote(string userId, bool isFor)
	{
		
	}
}
