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
		public BlobConstructs blobType;

		public LightSprite lightSprite = null;

		public SpriteHandler spriteHandler = null;

		public SpriteDataSO activeSprite = null;

		[Tooltip("Used for inactive or damaged sprites")]
		public SpriteDataSO inactiveSprite = null;

		[NonSerialized]
		public Integrity integrity;

		[NonSerialized]
		public List<Vector2Int> expandCoords = new List<Vector2Int>();

		[NonSerialized]
		public List<Vector2Int> healthPulseCoords = new List<Vector2Int>();

		[NonSerialized]
		public bool nodeDepleted;

		[NonSerialized]
		public Vector3Int location;

		public string overmindName;

		[NonSerialized]
		public Armor initialArmor;

		[NonSerialized]
		public Resistances initialResistances;

		private bool initialSet;

		[NonSerialized]
		public bool connectedToBlobNet;

		[NonSerialized]
		public BlobStructure connectedNode;

		[NonSerialized]
		public List<Vector3Int> connectedPath = new List<Vector3Int>();

		[NonSerialized]
		public LineRenderer lineRenderer;

		private void Awake()
		{
			integrity = GetComponent<Integrity>();
			lineRenderer = GetComponent<LineRenderer>();

			if(initialSet || integrity == null) return;

			initialSet = true;
			initialArmor = integrity.Armor;
			initialResistances = integrity.Resistances;
		}

		private void OnDisable()
		{
			if(integrity == null) return;

			integrity.OnWillDestroyServer.RemoveAllListeners();
			integrity.OnApplyDamage.RemoveAllListeners();
		}
	}
}
