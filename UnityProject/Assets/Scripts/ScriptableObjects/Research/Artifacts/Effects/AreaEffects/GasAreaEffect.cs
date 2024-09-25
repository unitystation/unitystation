using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using Systems.Atmospherics;
using ScriptableObjects.Atmospherics;
using NaughtyAttributes;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "GasAreaEffect", menuName = "ScriptableObjects/Systems/Artifacts/GasAreaEffect")]
	public class GasAreaEffect : AreaEffectBase
	{
		[SerializeField] bool AddGas = false;
		[SerializeField, ShowIf("AddGas")] GasSO GasToAdd = null;
		[SerializeField, ShowIf("AddGas")] float MolesToAdd = 0f;

		[SerializeField] bool RemoveGas = false;
		[SerializeField, ShowIf("RemoveGas")] GasSO GasToRemove = null;
		[SerializeField, ShowIf("RemoveGas")] float MolesToRemove = 0f;

		[SerializeField] bool ChangeTemperature = false;
		[SerializeField, ShowIf("ChangeTemperature")] float TemperatureChange = 0f;

		public override void DoEffectAura(GameObject centeredAround)
		{
			UniversalObjectPhysics objectPhysics = centeredAround.GetComponent<UniversalObjectPhysics>();
			Vector3Int position = objectPhysics.registerTile.WorldPosition;

			MetaDataNode node = MatrixManager.GetMetaDataAt(position);

			if(RemoveGas) node.GasMixLocal.RemoveGas(GasToRemove, MolesToRemove);

			if(ChangeTemperature) node.GasMixLocal.ChangeTemperature(TemperatureChange);

			if(AddGas) node.GasMixLocal.AddGasWithTemperature(GasToAdd, MolesToAdd,node.GasMixLocal.Temperature);
		}
	}
}
