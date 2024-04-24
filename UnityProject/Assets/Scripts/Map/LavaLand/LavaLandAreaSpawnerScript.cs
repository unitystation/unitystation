using System;
using System.Collections;
using System.Collections.Generic;
using InGameGizmos;
using Mirror;
using ScriptableObjects;
using TileMap.Behaviours;
using UnityEngine;

namespace Systems.Scenes
{
	public class LavaLandAreaSpawnerScript : ItemMatrixSystemInit, ISelectionGizmo
	{
		[SyncVar]
		public AreaSizes Size;

		[SyncVar]
		public bool allowSpecialSites;

		public GameGizmoSquare GameGizmoSquare;
		private void Start()
		{
			LavaLandManager.Instance.SpawnScripts.Add(this, Size);
		}

		private Vector3 GizmoSize()
		{
			switch (Size)
			{
				case AreaSizes.FiveByFive:
					return Vector3.one * 5;
				case AreaSizes.TenByTen:
					return Vector3.one * 10;
				case AreaSizes.FifteenByFifteen:
					return Vector3.one * 15;
				case AreaSizes.TwentyByTwenty:
					return Vector3.one * 20;
				case AreaSizes.TwentyfiveByTwentyfive:
					return Vector3.one * 25;
			}

			return Vector3.one;
		}

		public void OnSelected()
		{
			GameGizmoSquare.OrNull()?.Remove();
			GameGizmoSquare = GameGizmomanager.AddNewSquareStaticClient(this.gameObject, Vector3.zero, Color.green, BoxSize: GizmoSize());
		}

		public void OnDeselect()
		{
			GameGizmoSquare.OrNull()?.Remove();
			GameGizmoSquare = null;
		}

		public void UpdateGizmos()
		{
			GameGizmoSquare.transform.localScale = GizmoSize();
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position , GizmoSize());
		}

	}
}
