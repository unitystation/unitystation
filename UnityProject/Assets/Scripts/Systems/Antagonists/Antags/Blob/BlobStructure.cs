using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blob
{
	public class BlobStructure : MonoBehaviour
	{
		public bool isCore;
		public bool isNode;
		public bool isResource;
		public bool isFactory;
		public bool isReflective;
		public bool isStrong;
		public bool isNormal;

		private Integrity integrity;

		[HideInInspector]
		public List<Vector2Int> expandCoords = new List<Vector2Int>();

		[HideInInspector]
		public bool nodeDepleted;

		[HideInInspector]
		public Vector3Int location;

		private void Start()
		{
			integrity = GetComponent<Integrity>();
		}
	}
}
