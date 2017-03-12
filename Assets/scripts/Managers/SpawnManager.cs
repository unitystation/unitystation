using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayGroup
{
	public class SpawnManager : MonoBehaviour
	{
		public Transform playerPrefab;
		public Transform spawnPoint;

		public void SpawnPlayer()
		{
			//TODO: More spawn points and a way to iterate through them
			PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, Quaternion.identity, 0); 

		}
	}
}
