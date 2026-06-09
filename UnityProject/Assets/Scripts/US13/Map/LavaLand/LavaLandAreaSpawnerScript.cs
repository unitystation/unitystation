using Mirror;
using UnityEngine;
using US13.Core.GameGizmos;
using US13.Managers;
using US13.ScriptableObjects;
using US13.Tilemaps.Behaviours.Meta;
using US13.Variable_Viewer;
using Util;

namespace US13.Map.LavaLand
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
