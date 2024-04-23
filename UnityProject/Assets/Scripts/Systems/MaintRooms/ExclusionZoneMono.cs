using System.Collections;
using System.Collections.Generic;
using InGameGizmos;
using Mirror;
using Shared.Systems.ObjectConnection;
using Systems.Scenes;
using UnityEngine;

namespace MaintRooms
{
	public class ExclusionZoneMono : NetworkBehaviour, IMultitoolSlaveable, ISelectionGizmo
	{
		public MultitoolConnectionType ConType => MultitoolConnectionType.MaintGeneratorExclusionZone;

		public bool CanRelink => true;

		[SerializeField, SyncVar(hook = nameof(SyncMaintGenerator))]
		private NetworkBehaviour maintGenerator;

		private const int WALL_GAP = 2;
		private readonly Vector3 GIZMO_OFFSET = new Vector3(-0.5f, -0.5f, 0);


		public Vector2Int Offset;
		public Vector2Int Size;

		private GameGizmoSquare GameGizmoSquare;

		public IMultitoolMasterable Master
		{
			get => (MaintGenerator) maintGenerator;
			set { maintGenerator = (MaintGenerator) value; }
		}


		public void SyncMaintGenerator(NetworkBehaviour OldNB, NetworkBehaviour NewNB)
		{
			maintGenerator = NewNB;

			if (OldNB != null)
			{
				if (GameGizmoSquare != null)
				{
					this.OnDeselect();
				}

				((MaintGenerator) OldNB).RemoveExclusionZoneMono(this);
			}

			if (NewNB != null)
			{
				if (((MaintGenerator) NewNB).GameGizmoSquare != null)
				{
					this.OnSelected();
				}

				((MaintGenerator) NewNB).AddExclusionZoneMono(this);
			}
		}

		public bool RequireLink => true;

		public bool TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			Master = master;
			return true;
		}

		public void SetMasterEditor(IMultitoolMasterable master)
		{
			if (Master != null)
			{
				var MaintGenerator = (Master as MaintGenerator);

				MaintGenerator.RemoveExclusionZoneMono(this);
			}

			Master = master;
			if (Master != null)
			{
				var MaintGenerator = (Master as MaintGenerator);
				MaintGenerator.AddExclusionZoneMono(this);
			}
		}


		private void OnDrawGizmos()
		{
			Gizmos.color = Color.cyan;

			Gizmos.DrawWireCube(transform.position + Offset.To3() + Size.To3() / WALL_GAP + GIZMO_OFFSET, Size.To3());
		}


		public void OnSelected()
		{
			GameGizmoSquare.OrNull()?.Remove();
			GameGizmoSquare = GameGizmomanager.AddNewSquareStaticClient(this.gameObject,
				Offset.To3() + Size.To3() / WALL_GAP + GIZMO_OFFSET, Color.cyan, BoxSize: Size);
		}

		public void OnDeselect()
		{
			GameGizmoSquare.OrNull()?.Remove();
			GameGizmoSquare = null;
		}

		public void UpdateGizmos()
		{
			GameGizmoSquare.Position = Offset.To3() + Size.To3() / WALL_GAP + GIZMO_OFFSET;
			GameGizmoSquare.transform.localScale = Size.To3();
		}
	}
}