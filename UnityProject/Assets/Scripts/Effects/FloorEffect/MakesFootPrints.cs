using System.Linq;
using Chemistry;
using Chemistry.Components;
using UnityEngine;

namespace Effects.FloorEffect
{
	public class MakesFootPrints : MonoBehaviour, IServerInventoryMove
	{
		public ReagentContainer spillContents;
		private PlayerScript me;
		private Vector3Int oldPosition;

		[SerializeField] private GameObject FootprintTile;

		#region Lifecycle

		public void Awake()
		{
			//spillContents = gameObject.GetComponent<ReagentContainer>();
			oldPosition = gameObject.AssumedWorldPosServer().RoundToInt();
			me = GetComponentInParent<PlayerScript>();
			Debug.Log(me);
		}

		public void OnDestroy()
		{
			if (me != null)
			{
				me.playerMove.OnLocalTileReached.RemoveListener(LocalTileReached);
			}
			me = null;
		}

		#endregion Lifecycle

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (me != null)
			{
				me.playerMove.OnLocalTileReached.RemoveListener(LocalTileReached);
			}


			if (info.ToRootPlayer.OrNull()?.PlayerScript != null)
			{
				me = info.ToRootPlayer.PlayerScript;
				me.playerMove.OnLocalTileReached.AddListener(LocalTileReached);
			}


		}


		public void LocalTileReached(Vector3Int old,Vector3Int newPosition )
		{
			if (spillContents.ReagentMixTotal > 0f)
			{
				bool useAll = spillContents.ReagentMixTotal < 0.1f;

				Vector3Int currentPosition = gameObject.AssumedWorldPosServer().RoundToInt(); //AssumedWorldPosServer Really doing the heavy lifting here amazing
				if (MatrixManager.IsSpaceAt(oldPosition, true) == false)
				{
					var decals = MatrixManager.GetAt<FloorPrintEffect>(oldPosition, isServer: true);
					if (decals.Any())
					{
						var floorPrintEffect = decals.First();

						var change = currentPosition.ToLocal(me.RegisterPlayer.Matrix) - oldPosition.ToLocal(me.RegisterPlayer.Matrix);
						floorPrintEffect.RegisterLeave(	Orientation.FromAsEnum(change));
					}
				}


				if (currentPosition != oldPosition &&
				    MatrixManager.IsSpaceAt(gameObject.AssumedWorldPosServer().RoundToInt(), true)== false)
				{

					var reagents = spillContents.TakeReagents(
						useAll ? spillContents.ReagentMixTotal : spillContents.ReagentMixTotal * 0.25f); //Use all use everything in the container if not only 10%

					var decals = MatrixManager.GetAt<FloorPrintEffect>(currentPosition, isServer: true);
					if (decals.Any())
					{
						MatrixManager.ReagentReact(reagents,
							gameObject.AssumedWorldPosServer().RoundToInt(), null, false, me.CurrentDirection);

						var floorPrintEffect = decals.First();
						var Change = currentPosition.ToLocal(me.RegisterPlayer.Matrix) - oldPosition.ToLocal(me.RegisterPlayer.Matrix);
						floorPrintEffect.RegisterEnter(	Orientation.FromAsEnum(Change));
					}
					else
					{
						var footPrint = FootPrint(gameObject.AssumedWorldPosServer().RoundToInt(), reagents);

						MatrixManager.ReagentReact(reagents,
							gameObject.AssumedWorldPosServer().RoundToInt(), null, false, me.CurrentDirection);

						var change = currentPosition.ToLocal(me.RegisterPlayer.Matrix) - oldPosition.ToLocal(me.RegisterPlayer.Matrix);
						footPrint.RegisterEnter(Orientation.FromAsEnum(change));
					}

					oldPosition = currentPosition;
				}
			}
		}

		public FloorPrintEffect FootPrint(Vector3Int worldPos, ReagentMix reagents)
		{
			//No existing decal tile, lets make one
			var footTileInst = Spawn.ServerPrefab(FootprintTile, worldPos, MatrixManager.AtPoint(worldPos, true).Objects,
				Quaternion.identity).GameObject; ;
			return footTileInst.GetComponent<FloorPrintEffect>();
		}
	}
}