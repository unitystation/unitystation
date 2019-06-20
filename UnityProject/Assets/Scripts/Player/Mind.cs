using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// IC character information (job role, antag info, real name, etc). A body and their ghost link to the same mind
/// </summary>
public class Mind
{
	public JobType jobType = JobType.NULL;
	public PlayerScript ghost;
	public PlayerScript body;
	public bool IsGhosting;

	public void ClonePlayer(GameObject spawnPoint, CharacterSettings characterSettings)
	{
		GameObject oldBody;
		if(IsGhosting)
		{
			oldBody = ghost.gameObject;
		}
		else
		{
			oldBody = body.gameObject;
		}
		var connection = oldBody.GetComponent<NetworkIdentity>().connectionToClient;
		var playerID = oldBody.GetComponent<NetworkBehaviour>().playerControllerId;
		//SpawnHandler.ClonePlayer(connection, playerID, jobType, characterSettings, oldBody, spawnPoint);
	}

	public void Ghosting(GameObject newGhost)
	{
		IsGhosting = true;
		var PS = newGhost.GetComponent<PlayerScript>();
		PS.mind = this;
		ghost = PS;
	}

	public void ReturnToBody()
	{
		IsGhosting = false;
	}
}
