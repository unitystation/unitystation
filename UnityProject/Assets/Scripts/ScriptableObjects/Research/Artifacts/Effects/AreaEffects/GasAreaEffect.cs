using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Atmospherics;
using ScriptableObjects.Atmospherics;
using NaughtyAttributes;

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

			if(RemoveGas) node.GasMix.RemoveGas(GasToRemove, MolesToRemove);

			if(ChangeTemperature) node.GasMix.ChangeTemperature(TemperatureChange);

			if(AddGas) node.GasMix.AddGas(GasToAdd, MolesToAdd);
		}
	}
}
