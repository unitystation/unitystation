using System.Linq;
using Chemistry;
using UnityEngine;
using US13.Core.Factories;
using US13.HealthV2.Living;
using US13.Managers.MatrixManager;
using US13.Objects.Construction.FloorDecals;
using Util;

namespace US13.ChemistryComponents.SplatEffects
{
	public class MakeSlipperyOnSplat : IOnSplatEffect
	{
		[SerializeField] private bool isSuperSlippery = false;
		[SerializeField] private bool canDryUp = false;

		public void HandleSplatForReagent(ref ReagentMix reagents, ref bool didSplat,
			Vector3 position, Vector3 worldPos, Vector3Int localPosInt, bool spawnPrefabEffect = true)
		{
			MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPos, true);
			matrixInfo.MetaDataLayer.MakeSlipperyAt(localPosInt, canDryUp, isSuperSlippery);
			didSplat = true;
			EffectsFactory.WaterSplat(worldPos);
		}
	}

	public class CleanOnSplat : IOnSplatEffect
	{
		public void HandleSplatForReagent(ref ReagentMix reagents, ref bool didSplat,
			Vector3 position, Vector3 worldPos, Vector3Int localPosInt, bool spawnPrefabEffect = true)
		{
			MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPos, true);
			matrixInfo.MetaDataLayer.Clean(worldPos, localPosInt, false);
		}
	}


	public class PaintBlood : IOnSplatEffect
	{
		public void HandleSplatForReagent(ref ReagentMix reagents, ref bool didSplat,
			Vector3 position, Vector3 worldPos, Vector3Int localPosInt, bool spawnPrefabEffect = true)
		{
			MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPos, true);
			var existingSplats = MatrixManager.GetAt<FloorDecal>(position, true);
			var splatList = existingSplats.ToList();

			var cell = matrixInfo.Matrix.GetMetaDataNode(localPosInt);
			var preexistingAmount = cell.ReagentsOnTile.Total;

			foreach (var decal in splatList)
			{
				preexistingAmount += decal.ReagentContainer.ReagentMixTotal;
			}

			if (preexistingAmount + reagents.Total <= 30)
			{
				if (preexistingAmount.Approx(0))
				{
					if (spawnPrefabEffect) matrixInfo.MetaDataLayer.PaintBlood(position, reagents);
				}
				else if (splatList.Count > 0) splatList[0].ReagentContainer.Add(reagents);

				return;
			}

			foreach (var decal in splatList)
			{
				reagents.Add(decal.ReagentContainer.CurrentReagentMix);
				decal.ReagentContainer.CurrentReagentMix.Clear();
			}
		}
	}

	public class ExtinguishOnSplat : IOnSplatEffect
	{
		public void HandleSplatForReagent(ref ReagentMix reagents, ref bool didSplat,
			Vector3 position, Vector3 worldPos, Vector3Int localPosInt, bool spawnPrefabEffect = true)
		{
			MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPos, true);
			matrixInfo.ReactionManager.ExtinguishHotspot(localPosInt);
			foreach (var livingHealthBehaviour in matrixInfo.Matrix.Get<LivingHealthMasterBase>(localPosInt, true))
			{
				livingHealthBehaviour.Extinguish();
			}
		}
	}
}