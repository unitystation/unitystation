using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Chemistry.Components;
using Detective;
using Items;
using NUnit.Framework;
using Objects.Construction;
using UnityEngine;

namespace Effects.FloorEffect
{
	public class MakesFootPrints : MonoBehaviour, IServerInventoryMove
	{
		public ReagentContainer spillContents;
		private PlayerScript me;
		private Vector3Int oldPosition;

		private Pickupable pickupable;

		[SerializeField] private GameObject FootprintTile;
		[SerializeField] private ItemTrait FilthBlocking;

		private System.Random RNG = new System.Random();
		public int ClueShoeImprintInverseChance = 75;

		#region Lifecycle

		public void Awake()
		{
			//spillContents = gameObject.GetComponent<ReagentContainer>();
			oldPosition = gameObject.AssumedWorldPosServer().RoundToInt();
			me = GetComponentInParent<PlayerScript>();
			pickupable = GetComponent<Pickupable>();
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
				if  (IsValidSetup(info.ToRootPlayer))
				{
					me = info.ToRootPlayer.PlayerScript;
					me.playerMove.OnLocalTileReached.AddListener(LocalTileReached);
				}
			}


		}

		public bool IsValidSetup(RegisterPlayer player)
		{
			if (player == null) return false;
			// Checks if it's not null and checks if NamedSlot == NamedSlot The correct oone
			return player.PlayerScript.RegisterPlayer == pickupable.ItemSlot.Player && IsInCorrectNamedSlot();
		}

		/// <summary>
		/// Checks if the item is in the correct ItemSlot which is the eyes.
		/// Automatically returns false if null because of the "is" keyword and null propagation.
		/// </summary>
		private bool IsInCorrectNamedSlot()
		{
			return pickupable.ItemSlot is { NamedSlot: NamedSlot.feet };
		}

		public void LocalTileReached(Vector3Int old,Vector3Int newPosition )
		{
			if (spillContents.ReagentMixTotal <= 0f) return;
			bool useAll = spillContents.ReagentMixTotal < 0.1f;

			Vector3Int currentPosition = gameObject.AssumedWorldPosServer().RoundToInt(); //AssumedWorldPosServer Really doing the heavy lifting here amazing
			if (MatrixManager.IsSpaceAt(oldPosition, true) == false)
			{
				var decals = MatrixManager.GetAt<FloorPrintEffect>(oldPosition, isServer: true);
				if (decals.Any())
				{
					var floorPrintEffect = decals.First();

					var change = currentPosition.ToLocal(me.RegisterPlayer.Matrix) - oldPosition.ToLocal(me.RegisterPlayer.Matrix);
					floorPrintEffect.RegisterLeave(Orientation.FromAsEnum(change));
				}
			}

			if (currentPosition != oldPosition &&
			    MatrixManager.IsSpaceAt(currentPosition, true) == false)
			{
				var reagents = spillContents.TakeReagents(
					useAll ? spillContents.ReagentMixTotal : spillContents.ReagentMixTotal * 0.25f);

				var allComponents = MatrixManager.GetAt<CommonComponents>(currentPosition, true);
				var decals = new List<FloorPrintEffect>();
				var filteredComponents = new List<CommonComponents>();

				FilterComponents(ref allComponents, ref decals, ref filteredComponents);

				var localChange = currentPosition.ToLocal(me.RegisterPlayer.Matrix) - oldPosition.ToLocal(me.RegisterPlayer.Matrix);
				var orientation = Orientation.FromAsEnum(localChange);

				if (decals.Any())
				{
					MatrixManager.ReagentReact(reagents, currentPosition, null, false, me.CurrentDirection);
					decals.First().RegisterEnter(orientation);
				}
				else if (filteredComponents.Count == 0)
				{
					var footPrint = FootPrint(currentPosition, reagents);
					MatrixManager.ReagentReact(reagents, currentPosition, null, false, me.CurrentDirection);
					footPrint.RegisterEnter(orientation);
				}

				oldPosition = currentPosition;
			}
		}

		private void FilterComponents(ref IEnumerable<CommonComponents> allComponents, ref List<FloorPrintEffect> decals, ref List<CommonComponents> filteredComponents)
		{
			if (FilthBlocking == null) return;
			foreach (var comp in allComponents)
			{
				bool isBlocked = comp.TrySafeGetComponent<Attributes>(out var attr) &&
				                 attr.InitialTraits.Contains(FilthBlocking) == false;

				if (isBlocked) continue;

				if (comp.TryGetComponentCustom<FloorPrintEffect>(out var effect))
				{
					decals.Add(effect);
				}
				else
				{
					filteredComponents.Add(comp);
				}
			}
		}

		public FloorPrintEffect FootPrint(Vector3Int worldPos, ReagentMix reagents)
		{
			//No existing decal tile, lets make one
			var footTileInst = Spawn.ServerPrefab(FootprintTile, worldPos, MatrixManager.AtPoint(worldPos, true).Objects,
				Quaternion.identity).GameObject;
			if (RNG.Next(0, 100) > ClueShoeImprintInverseChance)
			{
				footTileInst.GetComponent<Attributes>().AppliedDetails.AddDetail(new Detail()
				{
					DetailType =  DetailType.Footprints,
					Description = "A shoe print",
					CausedByInstanceID = this.gameObject.GetInstanceID()
				});
			}

			return footTileInst.GetComponent<FloorPrintEffect>();
		}
	}
}