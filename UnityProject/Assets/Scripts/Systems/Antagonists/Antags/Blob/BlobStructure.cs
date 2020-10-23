using System;
using System.Collections;
using System.Collections.Generic;
using Light2D;
using UnityEngine;

namespace Blob
{
	/// <summary>
	/// Class used as a data holder on an object, all logic is in BlobPlayer
	/// </summary>
	public class BlobStructure : MonoBehaviour
	{
		public bool isCore;
		public bool isNode;
		public bool isResource;
		public bool isFactory;
		public bool isReflective;
		public bool isStrong;
		public bool isNormal;

		public LightSprite lightSprite = null;

		public SpriteHandler spriteHandler = null;

		[HideInInspector]
		public Integrity integrity;

		[HideInInspector]
		public List<Vector2Int> expandCoords = new List<Vector2Int>();

		[HideInInspector]
		public List<Vector2Int> healthPulseCoords = new List<Vector2Int>();

		[HideInInspector]
		public bool nodeDepleted;

		[HideInInspector]
		public Vector3Int location;

		public string overmindName;

		private void Start()
		{
			integrity = GetComponent<Integrity>();
		}

		private void OnDisable()
		{
			integrity.OnWillDestroyServer.RemoveAllListeners();
		}
	}
}
