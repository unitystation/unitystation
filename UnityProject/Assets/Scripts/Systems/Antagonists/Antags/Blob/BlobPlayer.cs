using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Blob
{
	/// <summary>
	/// Class which has the logic and data for the blob player
	/// </summary>
	public class BlobPlayer : NetworkBehaviour
	{
		[SerializeField]
		private GameObject blobCorePrefab = null;
		[SerializeField]
		private GameObject blobNodePrefab = null;
		[SerializeField]
		private GameObject blobResourcePrefab = null;
		[SerializeField]
		private GameObject blobFactoryPrefab = null;
		[SerializeField]
		private GameObject blobReflectivePrefab = null;
		[SerializeField]
		private GameObject blobStrongPrefab = null;
		[SerializeField]
		private GameObject blobNormalPrefab = null;

		private GameObject blobCore;

		private PlayerSync playerSync;

		[SyncVar(hook = nameof(SyncResources))]
		private int resources = 0;

		public void BlobStart()
		{
			playerSync = GetComponent<PlayerSync>();

			var result = Spawn.ServerPrefab(blobCorePrefab, playerSync.ServerPosition, gameObject.transform);

			if (!result.Successful)
			{
				Debug.LogError("Failed to spawn blob core for player!");
				return;
			}

			blobCore = result.GameObject;
		}

		private void SyncResources(int oldVar, int newVar)
		{
			resources = newVar;
		}
	}
}