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
	public bool DenyCloning;
	public int bodyMobID;

	public Mind(GameObject player, JobType newJobType)
	{
		jobType = newJobType;
		var playerScript = player.GetComponent<PlayerScript>();
		SetNewBody(playerScript);
	}

	public void SetNewBody(PlayerScript playerScript){
		playerScript.mind = this;
		body = playerScript;
		bodyMobID = playerScript.GetComponent<LivingHealthBehaviour>().mobID;
		StopGhosting();
	}

	public void ClonePlayer(GameObject spawnPoint, CharacterSettings characterSettings)
	{
		GameObject oldBody = GetCurrentMob();
		var connection = oldBody.GetComponent<NetworkIdentity>().connectionToClient;
		var playerID = oldBody.GetComponent<NetworkBehaviour>().playerControllerId;
		SpawnHandler.ClonePlayer(connection, playerID, jobType, characterSettings, oldBody, spawnPoint);
	}

	public GameObject GetCurrentMob()
	{
		if (IsGhosting)
		{
			return ghost.gameObject;
		}
		else
		{
			return body.gameObject;
		}
	}

	public void Ghosting(GameObject newGhost)
	{
		IsGhosting = true;
		var PS = newGhost.GetComponent<PlayerScript>();
		PS.mind = this;
		ghost = PS;
	}

	public void StopGhosting()
	{
		IsGhosting = false;
	}

	public bool ConfirmClone(int recordMobID)
	{
		if(bodyMobID != recordMobID){  //an old record might still exist even after several body swaps
			return false;
		}
		if(DenyCloning)
		{
			return false;
		}
		var currentMob = GetCurrentMob();
		if(!IsGhosting)
		{
			var livingHealthBehaviour = currentMob.GetComponent<LivingHealthBehaviour>();
			if(!livingHealthBehaviour.IsDead)
			{
				return false;
			}
		}
		if(!IsOnline(currentMob))
		{
			return false;
		}

		return true;
	}

	public bool IsOnline(GameObject currentMob)
	{
		NetworkConnection connection = currentMob.GetComponent<NetworkIdentity>().connectionToClient;
		if (PlayerList.Instance.ContainsConnection(connection) == false)
		{
			return false;
		}
		return true;
	}
}
